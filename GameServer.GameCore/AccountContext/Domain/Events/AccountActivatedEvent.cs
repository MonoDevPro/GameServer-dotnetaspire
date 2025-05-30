using GameServer.GameCore.AccountContext.Domain.Events.Base;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;

namespace GameServer.GameCore.AccountContext.Domain.Events;

public class AccountActivatedEvent : AccountEvent
{
    public string Username { get; }

    public AccountActivatedEvent(
        long accountId,
        UsernameVO username
        ) : base(accountId)
    {
        Username = username.Value;
    }
}