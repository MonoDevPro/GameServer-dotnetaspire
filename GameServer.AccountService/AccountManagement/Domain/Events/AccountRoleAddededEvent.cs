using GameServer.AccountService.AccountManagement.Domain.Events.Base;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;

namespace GameServer.AccountService.AccountManagement.Domain.Events;

public class AccountRoleAddedEvent : AccountEvent
{
    public string Role { get; }

    public AccountRoleAddedEvent(long accountId, RoleVO role) : base(accountId)
    {
        Role = role.Value;
    }
}