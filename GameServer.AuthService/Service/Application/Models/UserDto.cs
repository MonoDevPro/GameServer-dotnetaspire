
using GameServer.AuthService.Service.Domain.Entities;

namespace GameServer.AuthService.Service.Application.Models;

/// <summary>
/// Representa um usuário no sistema
/// </summary>
public class UserDto
{
    /// <summary>
    /// Identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// FirstName
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// LastName
    /// </summary>
    public string LastName { get; set; } = null!;
    
    public string Username { get; set; } = null!;

    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// User PhoneNumber
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Position Name
    /// </summary>
    public string? PositionName { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public UserDto(ApplicationUser userProfile)
    {
        Id = userProfile.Id;
        FirstName = userProfile.FirstName;
        LastName = userProfile.LastName;
        Username = userProfile.UserName;
        Email = userProfile.Email;
        PhoneNumber = userProfile.PhoneNumber;
        PositionName = userProfile.PositionName;
        IsActive = userProfile.IsActive;
        CreatedAt = userProfile.CreatedAt;
        LastLoginAt = userProfile.LastLoginAt;
    }
}