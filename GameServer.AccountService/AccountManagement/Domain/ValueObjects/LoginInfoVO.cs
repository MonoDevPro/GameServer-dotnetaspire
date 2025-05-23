using GameServer.AccountService.AccountManagement.Domain.ValueObjects.Base;

namespace GameServer.AccountService.AccountManagement.Domain.ValueObjects;

public sealed class LoginInfoVO : ValueObject<LoginInfoVO>
{
    public string LastLoginIp { get; }
    public DateTime LastLoginDate { get; private set; } = default!;

    private LoginInfoVO(string lastLoginIp, DateTime lastLoginDate)
    {
        LastLoginIp = lastLoginIp;
        LastLoginDate = lastLoginDate;
    }

    public static LoginInfoVO Create(string ipAddress, DateTime lastLoginDate)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP não pode ser vazio", nameof(ipAddress));

        return new LoginInfoVO(ipAddress, lastLoginDate);
    }

    protected override bool EqualsCore(LoginInfoVO? other) =>
        LastLoginIp == other?.LastLoginIp &&
        LastLoginDate == other?.LastLoginDate;

    protected override int ComputeHashCode() =>
        HashCode.Combine(LastLoginIp, LastLoginDate);
}