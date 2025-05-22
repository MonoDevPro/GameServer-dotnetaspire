using GameServer.AccountService.AccountManagement.Domain.Events.Base;

namespace GameServer.AccountService.AccountManagement.Domain.Events;

public class AccountPasswordUpdated : AccountEvent
{
    public AccountPasswordUpdated(long accountId) : base(accountId)
    {
    }
}