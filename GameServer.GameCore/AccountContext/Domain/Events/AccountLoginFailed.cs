using GameServer.GameCore.AccountContext.Domain.Events.Base;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;

namespace GameServer.GameCore.AccountContext.Domain.Events;

public class AccountLoginFailed : AccountEvent
{
    public string Username { get; }
    public string IpAddress { get; }
    public DateTime LoginTime { get; }

    public AccountLoginFailed(
        long accountId,
        UsernameVO username,
        LoginInfoVO login
        ) : base(accountId)
    {
        Username = username.Value;
        IpAddress = login.LastLoginIp;
        LoginTime = login.LastLoginDate;
    }
}