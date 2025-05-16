using GameServer.CharacterService.Domain.Entities;

namespace GameServer.CharacterService.Application.Ports.Out;

/// <summary>
/// Interface para o serviço de cache de personagens
/// </summary>
public interface ICharacterCache
{
    /// <summary>
    /// Tenta obter um personagem do cache pelo ID
    /// </summary>
    /// <param name="id">ID do personagem</param>
    /// <returns>O personagem se estiver no cache, ou nulo</returns>
    Task<Character?> GetCharacterByIdAsync(Guid id);
    
    /// <summary>
    /// Tenta obter um personagem do cache pelo nome
    /// </summary>
    /// <param name="name">Nome do personagem</param>
    /// <returns>O personagem se estiver no cache, ou nulo</returns>
    Task<Character?> GetCharacterByNameAsync(string name);
    
    /// <summary>
    /// Tenta obter um personagem do cache pelo ID do usuário proprietário
    /// </summary>
    /// <param name="userId">ID do usuário proprietário</param>
    /// <returns>Lista de personagens do usuário se estiver no cache, ou nulo</returns>
    Task<IEnumerable<Character>?> GetCharactersByUserIdAsync(Guid userId);
    
    /// <summary>
    /// Armazena ou atualiza um personagem no cache
    /// </summary>
    /// <param name="character">Personagem a ser armazenado</param>
    Task SetCharacterAsync(Character character);
    
    /// <summary>
    /// Remove um personagem do cache
    /// </summary>
    /// <param name="id">ID do personagem a ser removido</param>
    Task RemoveCharacterAsync(Guid id);
    
    /// <summary>
    /// Tenta obter um item do inventário do cache pelo ID
    /// </summary>
    /// <param name="id">ID do item</param>
    /// <returns>O item se estiver no cache, ou nulo</returns>
    Task<InventoryItem?> GetInventoryItemByIdAsync(Guid id);
    
    /// <summary>
    /// Tenta obter itens do inventário pelo ID do personagem
    /// </summary>
    /// <param name="characterId">ID do personagem</param>
    /// <returns>Lista de itens do personagem se estiver no cache, ou nulo</returns>
    Task<IEnumerable<InventoryItem>?> GetInventoryItemsByCharacterIdAsync(Guid characterId);
    
    /// <summary>
    /// Armazena ou atualiza um item do inventário no cache
    /// </summary>
    /// <param name="item">Item a ser armazenado</param>
    Task SetInventoryItemAsync(InventoryItem item);
    
    /// <summary>
    /// Remove um item do inventário do cache
    /// </summary>
    /// <param name="id">ID do item a ser removido</param>
    Task RemoveInventoryItemAsync(Guid id);
}