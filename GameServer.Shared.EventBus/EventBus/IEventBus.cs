using GameServer.Shared.EventBus.Events;

namespace GameServer.Shared.EventBus.EventBus;

/// <summary>
/// Simple in-process event bus interface.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribe to events of type TEvent
    /// </summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
    where TEvent : DomainEvent;

    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event)
    where TEvent : DomainEvent;
}