namespace GameServer.Shared.Database.Repository.UnityOfWork;

/// <summary>
/// Changes Tracking Type for DbSet operations
/// </summary>
public enum TrackingType
{
    NoTracking,
    NoTrackingWithIdentityResolution,
    Tracking
}