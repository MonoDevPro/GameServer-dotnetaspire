using GameServer.GameCore.AccountContext.Domain.Entities;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;

namespace GameServer.GameCore.AccountContext.Ports.Out.Persistence;

public interface IAccountRepositoryWriter
{
    Task<bool> SaveAsync(Account account);
    Task<bool> DeleteAsync(Account account);
    Task<bool> UpdateAsync(Account account);
    Task<bool> UpdateEmailAsync(Account account, EmailVO email);
    Task<bool> UpdatePasswordAsync(Account account, PasswordVO password);
}