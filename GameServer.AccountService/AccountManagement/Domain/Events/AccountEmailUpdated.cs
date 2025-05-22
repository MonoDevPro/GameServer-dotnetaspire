using GameServer.AccountService.AccountManagement.Domain.Events.Base;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;

namespace GameServer.AccountService.AccountManagement.Domain.Events;

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