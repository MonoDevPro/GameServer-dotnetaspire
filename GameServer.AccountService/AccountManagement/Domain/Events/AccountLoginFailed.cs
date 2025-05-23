using GameServer.AccountService.AccountManagement.Domain.Events.Base;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;

namespace GameServer.AccountService.AccountManagement.Domain.Events;

public class AccountLoginFailed : AccountEvent
{
    public string Username { get; }
    public string IpAddress { get; }
    public string DeviceInfo { get; }
    public DateTime LoginTime { get; }

    public AccountLoginFailed(
        long accountId,
        UsernameVO username, 
        LoginInfoVO login
        ) : base(accountId)
    {
        Username = username.Value;
        IpAddress = login.IpAddress;
        DeviceInfo = login.DeviceInfo;
        LoginTime = login.LoginTime;
    }
}