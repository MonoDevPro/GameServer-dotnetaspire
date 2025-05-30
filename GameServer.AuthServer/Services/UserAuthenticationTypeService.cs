using Microsoft.AspNetCore.Identity;

namespace GameServer.AuthServer.Services;

public class UserAuthenticationTypeService : IUserAuthenticationTypeService
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public UserAuthenticationTypeService(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> HasLocalPasswordAsync(IdentityUser<Guid> user)
    {
        return await _userManager.HasPasswordAsync(user);
    }

    public async Task<bool> IsOAuthOnlyUserAsync(IdentityUser<Guid> user)
    {
        var hasPassword = await _userManager.HasPasswordAsync(user);
        var externalLogins = await _userManager.GetLoginsAsync(user);

        // Usuário é OAuth-only se não tem senha local mas tem logins externos
        return !hasPassword && externalLogins.Any();
    }

    public async Task<List<string>> GetExternalLoginsAsync(IdentityUser<Guid> user)
    {
        var logins = await _userManager.GetLoginsAsync(user);
        return logins.Select(l => l.LoginProvider).ToList();
    }
}
