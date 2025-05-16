using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// Implementação de repositório de itens de inventário que utiliza cache
/// </summary>
public class CachedInventoryRepository : IInventoryRepository
{
    private readonly IInventoryRepository _repository;
    private readonly ICharacterCache _cache;
    private readonly ILogger<CachedInventoryRepository> _logger;

    public CachedInventoryRepository(
        IInventoryRepository repository,
        ICharacterCache cache,
        ILogger<CachedInventoryRepository> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id)
    {
        // Tenta obter do cache primeiro
        var item = await _cache.GetInventoryItemByIdAsync(id);
        if (item != null)
        {
            _logger.LogDebug("Item de inventário {ItemId} obtido do cache", id);
            return item;
        }

        // Se não estiver no cache, busca do repositório
        item = await _repository.GetByIdAsync(id);

        // Se encontrado, armazena no cache para consultas futuras
        if (item != null)
        {
            await _cache.SetInventoryItemAsync(item);
            _logger.LogDebug("Item de inventário {ItemId} armazenado no cache após busca no repositório", id);
        }

        return item;
    }

    public async Task<IEnumerable<InventoryItem>> GetByCharacterIdAsync(Guid characterId)
    {
        // Tenta obter do cache primeiro
        var items = await _cache.GetInventoryItemsByCharacterIdAsync(characterId);
        if (items != null)
        {
            _logger.LogDebug("Itens de inventário do personagem {CharacterId} obtidos do cache", characterId);
            return items;
        }

        // Se não estiver no cache, busca do repositório
        items = await _repository.GetByCharacterIdAsync(characterId);

        // Armazena cada item no cache para consultas futuras
        foreach (var item in items)
        {
            await _cache.SetInventoryItemAsync(item);
        }
        
        _logger.LogDebug("{Count} itens de inventário do personagem {CharacterId} armazenados no cache após busca no repositório", 
            items.Count(), characterId);

        return items;
    }

    public async Task<bool> AddAsync(InventoryItem item)
    {
        // Cria o item no repositório
        var createdItemResult = await _repository.AddAsync(item);

        // Armazena no cache
        if (createdItemResult)
        {
            await _cache.SetInventoryItemAsync(item);
            _logger.LogDebug("Item de inventário {ItemId} criado e armazenado no cache", item.Id);
        }
        
        return createdItemResult;
    }

    public async Task<bool> UpdateAsync(InventoryItem item)
    {
        // Atualiza o item no repositório
        var updated = await _repository.UpdateAsync(item);
        
        // Se foi atualizado com sucesso, atualiza também no cache
        if (updated)
        {
            await _cache.SetInventoryItemAsync(item);
            _logger.LogDebug("Item de inventário {ItemId} atualizado no cache", item.Id);
        }
        
        return updated;
    }

    public async Task<bool> RemoveAsync(Guid id)
    {
        // Remove do repositório
        var deleted = await _repository.RemoveAsync(id);
        
        // Se foi deletado com sucesso, remove também do cache
        if (deleted)
        {
            await _cache.RemoveInventoryItemAsync(id);
            _logger.LogDebug("Item de inventário {ItemId} removido do cache", id);
        }
        
        return deleted;
    }

    public Task<int> GetCountByCharacterIdAsync(Guid characterId)
    {
        // Obtém a contagem total de itens no inventário de um personagem
        return _repository.GetCountByCharacterIdAsync(characterId);
    }
}