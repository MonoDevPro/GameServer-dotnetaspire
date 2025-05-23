using GameServer.AccountService.AccountManagement.Domain.Entities;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;
using GameServer.AccountService.AccountManagement.Ports.Out.Persistence;
using GameServer.AccountService.AccountManagement.Ports.Out.Security;
using Microsoft.EntityFrameworkCore;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;

public class AccountEfRepository : IAccountRepository
{
    private readonly AccountDbContext _dbContext;

    public AccountEfRepository(
        AccountDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Account?> GetByIdAsync(long id)
    {
        return await _dbContext.Accounts
            .Include(a => a.Roles)
            .FirstOrDefaultAsync(a => a.Id.Equals(id));
    }

    public async Task<bool> ExistsByIdAsync(long id)
    {
        return await _dbContext.Accounts.AnyAsync(a => a.Id.Equals(id));
    }

    public async Task<bool> ExistsByEmailAsync(EmailVO email)
    {
        return await _dbContext.Accounts.AnyAsync(a => a.Email.Value == email.Value);
    }

    public async Task<bool> ExistsByUsernameAsync(UsernameVO username)
    {
        return await _dbContext.Accounts.AnyAsync(a => a.Username.Value == username.Value);
    }

    public async Task<bool> ValidatePasswordAsync(UsernameVO username, PasswordVO password)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a =>
                a.Username.Value == username.Value &&
                a.Password.Hash == password.Hash);

        return account != null;
    }

    public async Task<bool> ValidatePasswordAsync(EmailVO email, PasswordVO password)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a =>
                a.Email.Value == email.Value &&
                a.Password.Hash == password.Hash);

        return account != null;
    }

    public async Task<bool> SaveAsync(Account account)
    {
        try
        {
            var entry = _dbContext.Entry(account);

            if (entry.State == EntityState.Detached)
            {
                var existingAccount = await _dbContext.Accounts
                    .Include(a => a.Roles)
                    .FirstOrDefaultAsync(a => a.Id.Equals(account.Id));

                if (existingAccount == null)
                {
                    // Nova conta
                    _dbContext.Accounts.Add(account);
                }
                else
                {
                    // Atualiza a conta existente
                    _dbContext.Entry(existingAccount).CurrentValues.SetValues(account);

                    // Sincroniza a coleção de roles
                    _dbContext.Entry(existingAccount).Collection(a => a.Roles).Load();

                    // Remove roles que não existem mais
                    var rolesToRemove = existingAccount.Roles
                        .Where(r => !account.Roles.Contains(r))
                        .ToList();

                    foreach (var role in rolesToRemove)
                    {
                        existingAccount.RemoveRole(role);
                    }

                    // Adiciona novas roles
                    foreach (var role in account.Roles)
                    {
                        if (!existingAccount.Roles.Contains(role))
                        {
                            existingAccount.AddRole(role);
                        }
                    }
                }
            }

            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}