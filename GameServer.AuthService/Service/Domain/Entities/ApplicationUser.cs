using Microsoft.AspNetCore.Identity;

namespace GameServer.AuthService.Service.Domain.Entities;

/// <summary>
/// Default user for application.
/// Add profile data for application users by adding properties to the ApplicationUser class
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// FirstName
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// LastName
    /// </summary>
    public string LastName { get; set; } = null!;
    /// <summary>
    /// Indica se o usuário está ativo
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Indica se o usuário está banido
    /// </summary>
    public bool IsBanned { get; set; }
    
    /// <summary>
    /// Data de criação do usuário
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Data da última atualização do usuário
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Data do último login do usuário
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Data até quando o usuário está banido
    /// </summary>
    public DateTime? BannedUntil { get; set; }
    
    /// <summary>
    /// Motivo do banimento
    /// </summary>
    public string? BanReason { get; set; }
    
    /// <summary>
    /// Position Name
    /// </summary>
    public string? PositionName { get; set; }
    
    public List<string>? Roles { get; set; }
    

    /// <summary>
    /// Profile identity
    /// </summary>
    public Guid? ApplicationUserProfileId { get; set; }

    /// <summary>
    /// User Profile
    /// </summary>
    public virtual ApplicationUserProfile? ApplicationUserProfile { get; set; }
}