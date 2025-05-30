using GameServer.GameCore.AccountContext.Domain.Events.Base;

namespace GameServer.GameCore.AccountContext.Domain.Events;

public class AccountLoggedOut : AccountEvent
{
    public AccountLoggedOut(long accountId) : base(accountId)
    {
    }
}