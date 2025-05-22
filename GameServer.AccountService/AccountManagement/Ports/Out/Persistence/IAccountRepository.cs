using GameServer.AccountService.AccountManagement.Domain.Entities;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;

namespace GameServer.AccountService.AccountManagement.Ports.Out.Persistence;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(long id);
    Task<bool> ExistsByIdAsync(long id);
    Task<bool> ExistsByEmailAsync(EmailVO email);
    Task<bool> ExistsByUsernameAsync(UsernameVO username);
    Task<bool> SaveAsync(Account account);
}