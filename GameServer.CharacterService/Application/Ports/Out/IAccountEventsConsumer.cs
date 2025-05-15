using GameServer.CharacterService.Application.Models;

namespace GameServer.CharacterService.Application.Ports.Out;

/// <summary>
/// Porta de saída para consumir eventos de conta do RabbitMQ
/// </summary>
public interface IAccountEventsConsumer
{
    /// <summary>
    /// Inicia o consumo de eventos
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Para o consumo de eventos
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Evento disparado quando uma conta é criada
    /// </summary>
    event EventHandler<AccountCreatedEvent> AccountCreated;
    
    /// <summary>
    /// Evento disparado quando uma conta é atualizada
    /// </summary>
    event EventHandler<AccountUpdatedEvent> AccountUpdated;
    
    /// <summary>
    /// Evento disparado quando o status de uma conta muda
    /// </summary>
    event EventHandler<AccountStatusChangedEvent> AccountStatusChanged;
}