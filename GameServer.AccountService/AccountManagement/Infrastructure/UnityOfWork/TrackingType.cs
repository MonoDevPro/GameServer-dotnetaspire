namespace GameServer.AccountService.AccountManagement.Adapters.Out.Persistence.UnityOfWork;

/// <summary>
/// Changes Tracking Type for DbSet operations
/// </summary>
public enum TrackingType
{
    NoTracking,
    NoTrackingWithIdentityResolution,
    Tracking
}