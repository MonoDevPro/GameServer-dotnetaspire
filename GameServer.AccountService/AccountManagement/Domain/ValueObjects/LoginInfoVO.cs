using GameServer.AccountService.AccountManagement.Domain.ValueObjects.Base;

namespace GameServer.AccountService.AccountManagement.Domain.ValueObjects;

public sealed class LoginInfoVO : ValueObject<LoginInfoVO>
{
    public string IpAddress { get; }
    public string DeviceInfo { get; }
    public DateTime LoginTime { get; }
    
    private LoginInfoVO(string ipAddress, string deviceInfo)
    {
        IpAddress = ipAddress;
        DeviceInfo = deviceInfo;
        LoginTime = DateTime.UtcNow;
    }
    
    public static LoginInfoVO Create(string ipAddress, string deviceInfo)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP não pode ser vazio", nameof(ipAddress));
            
        return new LoginInfoVO(ipAddress, deviceInfo ?? "Desconhecido");
    }
    
    protected override bool EqualsCore(LoginInfoVO? other) => 
        IpAddress == other?.IpAddress && 
        DeviceInfo == other.DeviceInfo;
        
    protected override int ComputeHashCode() => 
        HashCode.Combine(IpAddress, DeviceInfo);
}