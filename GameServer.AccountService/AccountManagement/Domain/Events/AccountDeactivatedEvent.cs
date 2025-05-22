using GameServer.AccountService.AccountManagement.Domain.Events.Base;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;

namespace GameServer.AccountService.AccountManagement.Domain.Events;

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