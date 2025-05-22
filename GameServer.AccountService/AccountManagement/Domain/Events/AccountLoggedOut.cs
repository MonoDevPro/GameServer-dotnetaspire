using GameServer.AccountService.AccountManagement.Domain.Events.Base;

namespace GameServer.AccountService.AccountManagement.Domain.Events;

public class AccountLoggedOut : AccountEvent
{
    public AccountLoggedOut(long accountId) : base(accountId)
    {
    }
}