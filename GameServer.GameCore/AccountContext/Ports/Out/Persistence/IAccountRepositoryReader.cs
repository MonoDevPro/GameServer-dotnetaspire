using GameServer.GameCore.AccountContext.Domain.Entities;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;

namespace GameServer.GameCore.AccountContext.Ports.Out.Persistence;

public interface IAccountRepositoryReader
{
    Task<Account?> GetByIdAsync(long id);
    Task<bool> ExistsByIdAsync(long id);
    Task<bool> ExistsByEmailAsync(EmailVO email);
    Task<bool> ExistsByUsernameAsync(UsernameVO username);
}