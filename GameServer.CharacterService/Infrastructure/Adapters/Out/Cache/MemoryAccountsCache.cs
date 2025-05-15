using System.Collections.Concurrent;
using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Cache;

/// <summary>
/// Implementação de cache em memória para dados de contas
/// </summary>
public class MemoryAccountsCache : IAccountsCache
{
    private readonly ConcurrentDictionary<Guid, AccountCache> _accounts = new();
    private readonly ILogger<MemoryAccountsCache> _logger;

    public MemoryAccountsCache(ILogger<MemoryAccountsCache> logger)
    {
        _logger = logger;
    }

    public Task<AccountCache?> GetByIdAsync(Guid id)
    {
        if (_accounts.TryGetValue(id, out var account))
        {
            _logger.LogDebug("Cache hit para conta: {AccountId}", id);
            return Task.FromResult<AccountCache?>(account);
        }
        
        _logger.LogDebug("Cache miss para conta: {AccountId}", id);
        return Task.FromResult<AccountCache?>(null);
    }

    public Task<bool> IsActiveAsync(Guid id)
    {
        if (_accounts.TryGetValue(id, out var account))
        {
            _logger.LogDebug("Cache hit para verificação de status de conta: {AccountId}, IsActive: {IsActive}", 
                id, account.IsActive);
            return Task.FromResult(account.IsActive);
        }
        
        // Se não temos a conta em cache, assumimos que ela está inativa
        // Isso força a camada de aplicação a lidar com essa situação
        _logger.LogWarning("Cache miss ao verificar status de conta: {AccountId}, assumindo inativa", id);
        return Task.FromResult(false);
    }

    public Task<bool> RemoveAsync(Guid id)
    {
        var result = _accounts.TryRemove(id, out _);
        
        if (result)
        {
            _logger.LogInformation("Conta removida do cache: {AccountId}", id);
        }
        else
        {
            _logger.LogWarning("Tentativa de remover conta inexistente do cache: {AccountId}", id);
        }
        
        return Task.FromResult(result);
    }

    public Task<bool> SetAsync(AccountCache account)
    {
        _accounts[account.Id] = account;
        _logger.LogInformation("Conta adicionada/atualizada no cache: {AccountId}", account.Id);
        return Task.FromResult(true);
    }
}