using GameServer.AccountService.AccountManagement.Domain.Events.Base;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;

namespace GameServer.AccountService.AccountManagement.Domain.Events;

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