namespace GameServer.CharacterService.Application.Models;

/// <summary>
/// Evento emitido quando uma conta é criada
/// </summary>
public record AccountCreatedEvent(
    Guid Id, 
    string Username,
    string Email, 
    bool IsActive,
    DateTime CreatedAt);

/// <summary>
/// Evento emitido quando uma conta é atualizada
/// </summary>
public record AccountUpdatedEvent(
    Guid Id,
    string Email,
    bool IsActive,
    DateTime UpdatedAt);
    
/// <summary>
/// Evento emitido quando uma conta é desativada ou banida
/// </summary>
public record AccountStatusChangedEvent(
    Guid Id,
    bool IsActive,
    bool IsBanned,
    string? BanReason,
    DateTime? BannedUntil,
    DateTime UpdatedAt);