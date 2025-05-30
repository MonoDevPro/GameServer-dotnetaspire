namespace GameServer.GameCore.AccountContext.Ports.Out.Messaging;

/// <summary>
/// Simple in-process event bus interface.
/// </summary>
public interface IAccountEventBus
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