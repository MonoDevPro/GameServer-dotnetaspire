using GameServer.GameCore.AccountContext.Domain.Events.Base;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;

namespace GameServer.GameCore.AccountContext.Domain.Events;

public class AccountDeactivatedEvent : AccountEvent
{
    public string Username { get; }

    public AccountDeactivatedEvent(
        long accountId, 
        UsernameVO username
        ) : base(accountId)
    {
        Username = username.Value;
    }
}