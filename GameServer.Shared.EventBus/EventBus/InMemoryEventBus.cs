using System.Collections.Concurrent;
using GameServer.Shared.EventBus.Events;

namespace GameServer.Shared.EventBus.EventBus;

/// <summary>
/// In-memory implementation of IEventBus
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers
        = new ConcurrentDictionary<Type, List<Delegate>>();

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : DomainEvent
    {
        var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }

        return new Subscription(() => Unsubscribe(handler));
    }

    public async Task PublishAsync<TEvent>(TEvent @event)
        where TEvent : DomainEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            // invoke all handlers in parallel
            var tasks = handlers
                .OfType<Func<TEvent, Task>>()  // cast to correct delegate
                .Select(h => h(@event));
            await Task.WhenAll(tasks);
        }
    }

    private void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : DomainEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            lock (handlers) { handlers.Remove(handler); }
        }
    }

    /// <summary>
    /// Helper for unsubscribing
    /// </summary>
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