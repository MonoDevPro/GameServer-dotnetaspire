using System.Collections.Concurrent;
using GameServer.AccountService.AccountManagement.Ports.Out.Messaging;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Messaging;

/// <summary>
/// In-memory implementation of IEventBus
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers
        = new ConcurrentDictionary<Type, List<Delegate>>();

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }

        return new Subscription(() => Unsubscribe(handler));
    }

    public Task PublishAsync<TEvent>(TEvent @event)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            // invoke all handlers in parallel
            var tasks = handlers
                .OfType<Func<TEvent, Task>>()  // cast to correct delegate
                .Select(h => h(@event));
            return Task.WhenAll(tasks);
        }

        return Task.CompletedTask;
    }

    private void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
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