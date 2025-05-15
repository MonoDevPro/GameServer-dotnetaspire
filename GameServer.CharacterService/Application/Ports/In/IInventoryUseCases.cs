using GameServer.CharacterService.Application.Models;

namespace GameServer.CharacterService.Application.Ports.In;

/// <summary>
/// Porta de entrada para operações relacionadas ao inventário
/// </summary>
public interface IInventoryUseCases
{
    /// <summary>
    /// Obtém o inventário completo de um personagem
    /// </summary>
    Task<InventoryResponse> GetInventory(Guid characterId);
    
    /// <summary>
    /// Adiciona um item ao inventário do personagem
    /// </summary>
    Task<InventoryItemResponse> AddItem(Guid characterId, AddItemRequest request);
    
    /// <summary>
    /// Remove um item (ou reduz sua quantidade) do inventário do personagem
    /// </summary>
    Task<bool> RemoveItem(Guid characterId, RemoveItemRequest request);
    
    /// <summary>
    /// Equipa um item
    /// </summary>
    Task<bool> EquipItem(Guid characterId, EquipItemRequest request);
    
    /// <summary>
    /// Desequipa um item
    /// </summary>
    Task<bool> UnequipItem(Guid characterId, UnequipItemRequest request);
}