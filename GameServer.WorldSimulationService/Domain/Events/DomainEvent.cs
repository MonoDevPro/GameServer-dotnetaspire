using System;

namespace GameServer.WorldSimulationService.Domain.Events;

/// <summary>
/// Classe base de todo evento de domínio: imutável, contém timestamp e, opcionalmente, um identificador único.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Identificador único do evento.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// Momento em que o evento ocorreu.
    /// </summary>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}