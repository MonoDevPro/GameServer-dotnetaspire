using System.Linq.Expressions;
using GameServer.GameCore.AccountContext.Domain.Entities;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;
using GameServer.GameCore.AccountContext.Ports.Out.Persistence;
using GameServer.Shared.Database.Repository.Reader;
using GameServer.Shared.Database.Repository.Writer;

namespace GameServer.GameCore.AccountContext.Adapters.Out.Persistence;

public class AccountRepository : IAccountRepositoryReader, IAccountRepositoryWriter
{
    private readonly IReaderRepository<Account> _readerRepository;
    private readonly IWriterRepository<Account> _writerRepository;
    private readonly ILogger _logger;

    public AccountRepository(
        IReaderRepository<Account> readerRepository,
        IWriterRepository<Account> writerRepository,
        ILogger<AccountRepository>? logger = null
        )
    {
        _readerRepository = readerRepository ?? throw new ArgumentNullException(nameof(readerRepository));
        _writerRepository = writerRepository ?? throw new ArgumentNullException(nameof(writerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Writer Methods

    public async Task<bool> SaveAsync(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        try 
        {
            await _writerRepository.AddAsync(account);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving account: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Account account)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(account);

            await _writerRepository.DeleteAsync(account);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        try
        {
            await _writerRepository.UpdateAsync(account);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> UpdateEmailAsync(Account account, EmailVO email)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            account.UpdateEmail(email);
            await _writerRepository.UpdateAsync(account);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account email: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> UpdatePasswordAsync(Account account, PasswordVO password)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(password);

        try
        {
            account.UpdatePassword(password);
            await _writerRepository.UpdateAsync(account);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account password: {Message}", ex.Message);
            return false;
        }
    }

    #endregion

    #region Reader Methods
    public async Task<Account?> GetByIdAsync(long id)
        => await _readerRepository.QuerySingleAsync(
            a => a.Id == id,
            a => a);

    public async Task<bool> ExistsByIdAsync(long id)
        => await _readerRepository.ExistsAsync(
            a => a.Id.Equals(id));

    public async Task<bool> ExistsByEmailAsync(EmailVO email)
        => await _readerRepository.ExistsAsync(a => a.Email.Value == email.Value);

    public async Task<bool> ExistsByUsernameAsync(UsernameVO username)
        => await _readerRepository.ExistsAsync(
            a => a.Username.Value == username.Value);
    
    public async Task<bool> ExistsByUsernameAndPasswordAsync(UsernameVO username, PasswordVO password)
        => await _readerRepository.ExistsAsync(a =>
                a.Username.Value == username.Value &&
                a.Password.Hash == password.Hash);
    
    public async Task<bool> ExistsByEmailAndPasswordAsync(EmailVO email, PasswordVO password)
        => await _readerRepository.ExistsAsync(a =>
            a.Email.Value == email.Value &&
            a.Password.Hash == password.Hash);
    #endregion
}