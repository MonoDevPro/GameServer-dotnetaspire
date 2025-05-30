using GameServer.GameCore.AccountContext.Domain.Enums;
using GameServer.GameCore.AccountContext.Domain.ValueObjects.Base;

namespace GameServer.GameCore.AccountContext.Domain.ValueObjects;

public sealed class BanInfoVO : ValueObject<BanInfoVO>
{
    public BanStatus Status { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? Reason { get; private set; }
    public long? BannedById { get; private set; }
    
    private BanInfoVO() { }

    private BanInfoVO(BanStatus status, DateTime? expiresAt, string? reason, long? bannedById)
    {
        Status = status;
        ExpiresAt = expiresAt;
        Reason = reason;
        BannedById = bannedById;
    }

    public static BanInfoVO NotBanned() => 
        new BanInfoVO(BanStatus.NotBanned, null, null, null);

    public static BanInfoVO TemporaryBan(string reason, DateTime expiresAt, long bannedById)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentNullException(nameof(reason));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Data de expiração deve ser no futuro");

        return new BanInfoVO(BanStatus.TemporaryBan, expiresAt, reason, bannedById);
    }

    public static BanInfoVO PermanentBan(string reason, long bannedById)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentNullException(nameof(reason));

        return new BanInfoVO(BanStatus.PermanentBan, null, reason, bannedById);
    }

    public bool IsActive() => 
        Status != BanStatus.NotBanned && 
        (Status == BanStatus.PermanentBan || ExpiresAt > DateTime.UtcNow);

    protected override bool EqualsCore(BanInfoVO? other) => 
        Status == other?.Status && 
        ExpiresAt == other.ExpiresAt && 
        Reason == other.Reason && 
        BannedById == other.BannedById;

    protected override int ComputeHashCode() => 
        HashCode.Combine(Status, ExpiresAt, Reason, BannedById);
}