using GameServer.AccountService.AccountManagement.Domain.Events.Base;

namespace GameServer.AccountService.AccountManagement.Ports.Out.Messaging;

/// <summary>
/// Simple in-process event bus interface.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribe to events of type TEvent
    /// </summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler);

    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event);
}