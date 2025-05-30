using Microsoft.AspNetCore.Identity;

namespace GameServer.AuthServer.Services;

public interface IUserAuthenticationTypeService
{
    Task<bool> HasLocalPasswordAsync(IdentityUser<Guid> user);
    Task<bool> IsOAuthOnlyUserAsync(IdentityUser<Guid> user);
    Task<List<string>> GetExternalLoginsAsync(IdentityUser<Guid> user);
}
