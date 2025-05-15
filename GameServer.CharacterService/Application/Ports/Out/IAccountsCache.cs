using GameServer.CharacterService.Domain.Entities;

namespace GameServer.CharacterService.Application.Ports.Out;

/// <summary>
/// Porta de saída para acesso ao cache de contas
/// </summary>
public interface IAccountsCache
{
    /// <summary>
    /// Obtém uma conta pelo ID
    /// </summary>
    Task<AccountCache?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Adiciona ou atualiza uma conta no cache
    /// </summary>
    Task<bool> SetAsync(AccountCache account);
    
    /// <summary>
    /// Remove uma conta do cache
    /// </summary>
    Task<bool> RemoveAsync(Guid id);
    
    /// <summary>
    /// Verifica se uma conta existe e está ativa
    /// </summary>
    Task<bool> IsActiveAsync(Guid id);
}