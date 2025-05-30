using GameServer.GameCore.AccountContext.Domain.Events.Base;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;

namespace GameServer.GameCore.AccountContext.Domain.Events;

public class AccountRoleAddedEvent : AccountEvent
{
    public string Role { get; }

    public AccountRoleAddedEvent(long accountId, RoleVO role) : base(accountId)
    {
        Role = role.Value;
    }
}