using GameServer.GameCore.AccountContext.Domain.Events.Base;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;

namespace GameServer.GameCore.AccountContext.Domain.Events;

public class AccountEmailUpdated : AccountEvent
{
    public string PreviousEmail { get; }
    public string NewEmail { get; }
    
    public AccountEmailUpdated(
        long accountId,
        EmailVO previousEmail,
        EmailVO newEmail
        ) : base(accountId)
    {
        PreviousEmail = previousEmail.Value;
        NewEmail = newEmail.Value;
    }
}