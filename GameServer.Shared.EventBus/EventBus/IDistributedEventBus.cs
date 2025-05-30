using GameServer.Shared.EventBus.Events;

namespace GameServer.Shared.EventBus.EventBus;

/// <summary>
/// Interface for distributed event bus across microservices
/// </summary>
public interface IDistributedEventBus : IEventBus
{
    /// <summary>
    /// Subscribe to events from other microservices
    /// </summary>
    Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler, string consumerGroup = "default")
        where TEvent : DomainEvent;

    /// <summary>
    /// Publish event to other microservices with delivery guarantees
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, PublishOptions? options = null)
        where TEvent : DomainEvent;

    /// <summary>
    /// Health check for the event bus connection
    /// </summary>
    Task<bool> IsHealthyAsync();
}

/// <summary>
/// Options for publishing events
/// </summary>
public class PublishOptions
{
    public bool RequireAcknowledgment { get; set; } = true;
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public int RetryAttempts { get; set; } = 3;
    public string? PartitionKey { get; set; }
}
