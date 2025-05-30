using GameServer.GameCore.AccountContext.Domain.Events.Base;

namespace GameServer.GameCore.AccountContext.Domain.Events;

public class AccountPasswordUpdated : AccountEvent
{
    public AccountPasswordUpdated(long accountId) : base(accountId)
    {
    }
}