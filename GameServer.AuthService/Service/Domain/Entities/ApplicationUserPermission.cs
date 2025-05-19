using GameServer.AuthService.Service.Domain.Entities.Base;

namespace GameServer.AuthService.Service.Domain.Entities;

/// <summary>
/// User permission
/// </summary>
public abstract class ApplicationUserPermission : Auditable
{
    /// <summary>
    /// Application User profile identifier
    /// </summary>
    public Guid ApplicationUserProfileId { get; set; }

    /// <summary>
    /// Application User Profile
    /// </summary>
    public virtual ApplicationUser? ApplicationUserProfile { get; set; }

    /// <summary>
    /// Authorize attribute policy name
    /// </summary>
    public string PolicyName { get; set; } = null!;

    /// <summary>
    /// Description for current permission
    /// </summary>
    public string? Description { get; set; }
}