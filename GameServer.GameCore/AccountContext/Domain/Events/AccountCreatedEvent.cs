using GameServer.GameCore.AccountContext.Domain.Events.Base;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;

namespace GameServer.GameCore.AccountContext.Domain.Events;

/// <summary>
/// Evento disparado quando um novo usuário é registrado no sistema
/// </summary>
public class AccountCreatedEvent : AccountEvent
{
    /// <summary>
    /// Email do usuário registrado
    /// </summary>
    public string Email { get; }
    
    /// <summary>
    /// Nome de usuário escolhido
    /// </summary>
    public string Username { get; }
    
    public AccountCreatedEvent(long accountId, EmailVO email, UsernameVO username) : base(accountId)
    {
        Email = email.Value;
        Username = username.Value;
    }
}