using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.Entities;
using GameServer.AccountService.AccountManagement.Domain.Entities;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;
using GameServer.AccountService.AccountManagement.Ports.Out.Identity;
using Microsoft.AspNetCore.Identity;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Identity
{
    public class AccountIdentitySyncService : IAccountIdentitySyncService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        
        public AccountIdentitySyncService(
            UserManager<ApplicationUser> userManager
        )
        {
            _userManager = userManager;
        }
        
        public async Task<ApplicationUser> SyncToIdentityAsync(Account account)
        {
            // Buscar ou criar usuário do Identity
            var user = await _userManager.FindByIdAsync(account.UniqueId.ToString()) 
                       ?? new ApplicationUser { Id = account.UniqueId };
            
            // Sincronizar propriedades básicas
            user.Email = account.Email.Value;
            user.UserName = account.Username.Value;
            user.PasswordHash = account.Password.Hash;
            user.EmailConfirmed = account.IsActive;
            user.LockoutEnd = account.BanInfo.IsActive() ? 
                account.BanInfo.ExpiresAt : null;
            
            // Sincronizar roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            var accountRoles = account.Roles.Select(r => r.Value).ToList();
            
            await _userManager.AddToRolesAsync(user, 
                accountRoles.Except(currentRoles));
            await _userManager.RemoveFromRolesAsync(user, 
                currentRoles.Except(accountRoles));
            
            // Salvar alterações
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            
            // Retornar o usuário atualizado
            return user;
        }
        
        public async Task UpdateUsernameAsync(Guid identityId, UsernameVO username)
        {
            // Verificando se o UsernameVO contém o ID da conta (dependendo da sua implementação)
            
            var user = await _userManager.FindByIdAsync(identityId.ToString());
            
            if (user is null) 
                throw new KeyNotFoundException($"Usuário '{identityId}' não encontrado.");
            
            user.UserName = username.Value;
            user.NormalizedUserName = _userManager.NormalizeName(username.Value);
            await _userManager.UpdateAsync(user);
        }

        public async Task UpdatePasswordAsync(Guid identityId, PasswordVO password)
        {
            // Verificando se o PasswordVO contém o ID da conta (dependendo da sua implementação)
        
            var user = await _userManager.FindByIdAsync(identityId.ToString());
            
            if (user is null) 
                throw new KeyNotFoundException($"Usuário '{identityId}' não encontrado.");
                
            await _userManager.RemovePasswordAsync(user);
            await _userManager.AddPasswordAsync(user, password.Hash);
        }

        public async Task UpdateEmailAsync(Guid identityId, EmailVO email)
        {
            // Verificando se o EmailVO contém o ID da conta (dependendo da sua implementação)
        
            var user = await _userManager.FindByIdAsync(identityId.ToString());
            
            if (user is null)
                throw new KeyNotFoundException($"Usuário '{identityId}' não encontrado.");
            
            var token = await _userManager.GenerateChangeEmailTokenAsync(user, email.Value);
            await _userManager.ChangeEmailAsync(user, email.Value, token);
        
            // Também atualizar o nome de usuário se ele for igual ao email anterior
            if (user.UserName == user.Email)
            {
                user.UserName = email.Value;
                user.NormalizedUserName = _userManager.NormalizeName(email.Value);
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task UpdateRolesAsync(Guid identityId, IEnumerable<RoleVO> roles)
        {
            if (roles is null)
                throw new ArgumentNullException(nameof(roles));

            var user = await _userManager.FindByIdAsync(identityId.ToString());
            if (user is null)
                throw new KeyNotFoundException($"Usuário '{identityId}' não encontrado.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            var newRoles     = roles.Select(r => r.Value).ToList();

            var comparer       = StringComparer.OrdinalIgnoreCase;
            var rolesToAdd     = newRoles.Except(currentRoles, comparer).ToArray();
            var rolesToRemove  = currentRoles.Except(newRoles,   comparer).ToArray();

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Falha ao adicionar roles: {string.Join(", ", addResult.Errors.Select(e => e.Description))}"
                    );
            }

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Falha ao remover roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}"
                    );
            }
        }
    }
}