using GameServer.AuthService.Service.Domain.Entities.Base;

namespace GameServer.AuthService.Service.Domain.Entities;

/// <summary>
/// Represent person with login information (ApplicationUser)
/// </summary>
public class ApplicationUserProfile : Auditable
{
    /// <summary>
    /// Account
    /// </summary>
    public virtual ApplicationUser ApplicationUser { get; set; } = null!;

    /// <summary>
    /// Microservice permission for policy-based authorization
    /// </summary>
    public ICollection<ApplicationUserPermission>? Permissions { get; set; }
}