using Microsoft.AspNetCore.Identity;

namespace GameServer.AuthService.Service.Domain.Entities
{
    /// <summary>
    /// Application role
    /// </summary>
    public class ApplicationUserRole : IdentityRole<Guid>
    {
    }
}