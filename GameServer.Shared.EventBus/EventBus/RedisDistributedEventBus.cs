using System.Collections.Concurrent;
using System.Text.Json;
using GameServer.Shared.EventBus.Events;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GameServer.Shared.EventBus.EventBus;

/// <summary>
/// Redis-based distributed event bus implementation
/// Optimized for gaming scenarios with low latency and high throughput
/// </summary>
public class RedisDistributedEventBus : IDistributedEventBus
{
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisDistributedEventBus> _logger;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly string _serviceInstanceId;

    public RedisDistributedEventBus(
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedEventBus> logger)
    {
        _database = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
        _logger = logger;
        _serviceInstanceId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }

        return new Subscription(() => Unsubscribe(handler));
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler, string consumerGroup = "default") 
        where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent).Name;
        var channelName = $"events:{eventType}";
        var streamName = $"stream:events:{eventType}";

        // Subscribe to real-time channel
        await _subscriber.SubscribeAsync(channelName, async (channel, message) =>
        {
            try
            {
                var @event = JsonSerializer.Deserialize<TEvent>(message!);
                if (@event != null)
                {
                    await handler(@event);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventType} from channel {Channel}", eventType, channel);
            }
        });

        // Process events from stream (for reliability and catch-up)
        _ = Task.Run(async () =>
        {
            var consumerGroupName = $"{consumerGroup}-{_serviceInstanceId}";
            
            try
            {
                // Create consumer group if it doesn't exist
                await _database.StreamCreateConsumerGroupAsync(streamName, consumerGroupName, "0", createStream: true);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                // Consumer group already exists
            }

            while (true)
            {
                try
                {
                    var messages = await _database.StreamReadGroupAsync(
                        streamName, 
                        consumerGroupName, 
                        _serviceInstanceId, 
                        ">", 
                        count: 10,
                        block: TimeSpan.FromSeconds(1));

                    foreach (var message in messages)
                    {
                        try
                        {
                            var eventData = message.Values.FirstOrDefault(v => v.Name == "data").Value;
                            if (eventData.HasValue)
                            {
                                var @event = JsonSerializer.Deserialize<TEvent>(eventData!);
                                if (@event != null)
                                {
                                    await handler(@event);
                                    // Acknowledge message
                                    await _database.StreamAcknowledgeAsync(streamName, consumerGroupName, message.Id);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing stream message {MessageId}", message.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from stream {StreamName}", streamName);
                    await Task.Delay(TimeSpan.FromSeconds(5)); // Back off on error
                }
            }
        });

        _logger.LogInformation("Subscribed to event {EventType} with consumer group {ConsumerGroup}", eventType, consumerGroup);
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
    {
        await PublishAsync(@event, new PublishOptions());
    }

    public async Task PublishAsync<TEvent>(TEvent @event, PublishOptions? options = null) where TEvent : DomainEvent
    {
        options ??= new PublishOptions();
        var eventType = typeof(TEvent).Name;
        var eventData = JsonSerializer.Serialize(@event);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            // Publish to real-time channel for immediate consumption
            var channelPublishTask = _subscriber.PublishAsync($"events:{eventType}", eventData);

            // Add to stream for reliability and replay capability
            var streamAddTask = _database.StreamAddAsync($"stream:events:{eventType}", new NameValueEntry[]
            {
                new("type", eventType),
                new("data", eventData),
                new("timestamp", timestamp),
                new("aggregateId", @event.AggregateId.ToString()),
                new("serviceId", _serviceInstanceId)
            });

            if (options.RequireAcknowledgment)
            {
                // Wait for both operations to complete
                await Task.WhenAll(channelPublishTask, streamAddTask);
            }
            else
            {
                // Fire and forget
                _ = Task.WhenAll(channelPublishTask, streamAddTask);
            }

            _logger.LogDebug("Published event {EventType} for aggregate {AggregateId}", eventType, @event.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} for aggregate {AggregateId}", eventType, @event.AggregateId);
            
            if (options.RequireAcknowledgment)
            {
                throw;
            }
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await _database.PingAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            lock (handlers) { handlers.Remove(handler); }
        }
    }

    private class Subscription : IDisposable
    {
        private readonly Action _dispose;
        private bool _isDisposed;

        public Subscription(Action dispose) => _dispose = dispose;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _dispose();
                _isDisposed = true;
            }
        }
    }
}
