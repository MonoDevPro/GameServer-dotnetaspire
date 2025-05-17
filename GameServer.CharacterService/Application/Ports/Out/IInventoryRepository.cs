using GameServer.CharacterService.Domain.Entities;

namespace GameServer.CharacterService.Application.Ports.Out;

/// <summary>
/// Porta de saída para acesso ao repositório de itens do inventário
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Obtém todos os itens do inventário de um personagem
    /// </summary>
    Task<IEnumerable<InventoryItem>> GetByCharacterIdAsync(Guid characterId);
    
    /// <summary>
    /// Obtém um item específico do inventário pelo seu ID
    /// </summary>
    Task<InventoryItem?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Adiciona um novo item ao inventário
    /// </summary>
    Task<InventoryItem?> AddAsync(InventoryItem item);
    
    /// <summary>
    /// Atualiza um item existente no inventário
    /// </summary>
    Task<InventoryItem?> UpdateAsync(InventoryItem item);
    
    /// <summary>
    /// Remove um item do inventário
    /// </summary>
    Task<bool> RemoveAsync(Guid id);
    
    /// <summary>
    /// Obtém a contagem total de itens no inventário de um personagem
    /// </summary>
    Task<int> GetCountByCharacterIdAsync(Guid characterId);
}