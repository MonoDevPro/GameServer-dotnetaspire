using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.Entities;
using GameServer.AccountService.AccountManagement.Domain.Entities;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;

namespace GameServer.AccountService.AccountManagement.Ports.Out.Identity;

public interface IAccountIdentitySyncService
{
    Task<ApplicationUser> SyncToIdentityAsync(Account account);
    Task UpdateUsernameAsync(Guid identityId, UsernameVO account);
    Task UpdatePasswordAsync(Guid identityId, PasswordVO account);
    Task UpdateEmailAsync(Guid identityId, EmailVO account);
    Task UpdateRolesAsync(Guid identityId, IEnumerable<RoleVO> roles);
}