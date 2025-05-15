using System.Text.Json;
using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Cache;

/// <summary>
/// Implementação do repositório de inventário usando Redis como cache
/// </summary>
public class RedisInventoryRepository : IInventoryRepository
{
    private readonly IInventoryRepository _decorated;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisInventoryRepository> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);
    
    private const string InventoryKeyPrefix = "inventory:";
    private const string ItemKeyPrefix = "item:";
    private const string CountKeyPrefix = "count:";

    public RedisInventoryRepository(
        IInventoryRepository decorated, 
        IDistributedCache cache, 
        ILogger<RedisInventoryRepository> logger)
    {
        _decorated = decorated;
        _cache = cache;
        _logger = logger;
    }

    private string GetInventoryKey(Guid characterId) => $"{InventoryKeyPrefix}{characterId}";
    private string GetItemKey(Guid itemId) => $"{ItemKeyPrefix}{itemId}";
    private string GetCountKey(Guid characterId) => $"{CountKeyPrefix}{characterId}";

    public async Task<InventoryItem> AddAsync(InventoryItem item)
    {
        // Primeiro, adiciona ao repositório subjacente
        var addedItem = await _decorated.AddAsync(item);
        
        // Invalidar o cache de inventário para forçar atualização em próxima consulta
        await InvalidateInventoryCache(item.CharacterId);
        
        _logger.LogInformation("Cache de inventário invalidado após adicionar item: {ItemId}, Personagem: {CharacterId}",
            item.Id, item.CharacterId);
        
        return addedItem;
    }

    public async Task<IEnumerable<InventoryItem>> GetByCharacterIdAsync(Guid characterId)
    {
        // Tentar obter do cache primeiro
        var key = GetInventoryKey(characterId);
        var cachedValue = await _cache.GetStringAsync(key);
        
        if (!string.IsNullOrEmpty(cachedValue))
        {
            try
            {
                _logger.LogInformation("Cache hit para inventário do personagem: {CharacterId}", characterId);
                return JsonSerializer.Deserialize<IEnumerable<InventoryItem>>(cachedValue) ?? Enumerable.Empty<InventoryItem>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar cache do inventário: {Message}", ex.Message);
                // Falha silenciosa e busca do repositório
            }
        }

        // Cache miss - buscar do repositório e armazenar em cache
        var items = await _decorated.GetByCharacterIdAsync(characterId);
        
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };
            
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(items), options);
            _logger.LogInformation("Inventário do personagem {CharacterId} armazenado em cache", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao armazenar inventário em cache: {Message}", ex.Message);
            // Falha silenciosa - retorna os dados mesmo sem cache
        }
        
        return items;
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id)
    {
        // Tentar obter do cache primeiro
        var key = GetItemKey(id);
        var cachedValue = await _cache.GetStringAsync(key);
        
        if (!string.IsNullOrEmpty(cachedValue))
        {
            try
            {
                _logger.LogInformation("Cache hit para item de inventário: {ItemId}", id);
                return JsonSerializer.Deserialize<InventoryItem>(cachedValue);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar cache do item: {Message}", ex.Message);
                // Falha silenciosa e busca do repositório
            }
        }

        // Cache miss - buscar do repositório e armazenar em cache
        var item = await _decorated.GetByIdAsync(id);
        if (item != null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheExpiration
                };
                
                await _cache.SetStringAsync(key, JsonSerializer.Serialize(item), options);
                _logger.LogInformation("Item de inventário {ItemId} armazenado em cache", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao armazenar item em cache: {Message}", ex.Message);
                // Falha silenciosa - retorna os dados mesmo sem cache
            }
        }
        
        return item;
    }

    public async Task<int> GetCountByCharacterIdAsync(Guid characterId)
    {
        // Tentar obter do cache primeiro
        var key = GetCountKey(characterId);
        var cachedValue = await _cache.GetStringAsync(key);
        
        if (!string.IsNullOrEmpty(cachedValue) && int.TryParse(cachedValue, out int count))
        {
            _logger.LogInformation("Cache hit para contagem de inventário do personagem: {CharacterId}", characterId);
            return count;
        }

        // Cache miss - buscar do repositório e armazenar em cache
        var countFromRepo = await _decorated.GetCountByCharacterIdAsync(characterId);
        
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };
            
            await _cache.SetStringAsync(key, countFromRepo.ToString(), options);
            _logger.LogInformation("Contagem de inventário do personagem {CharacterId} armazenada em cache", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao armazenar contagem em cache: {Message}", ex.Message);
            // Falha silenciosa - retorna os dados mesmo sem cache
        }
        
        return countFromRepo;
    }

    public async Task<bool> RemoveAsync(Guid id)
    {
        // Primeiro, busca o item para obter o CharacterId
        var item = await _decorated.GetByIdAsync(id);
        if (item == null) return false;
        
        // Remove do repositório subjacente
        var result = await _decorated.RemoveAsync(id);
        
        if (result)
        {
            // Remover do cache de item
            await _cache.RemoveAsync(GetItemKey(id));
            
            // Invalidar o cache de inventário
            await InvalidateInventoryCache(item.CharacterId);
            
            _logger.LogInformation("Cache de item e inventário invalidados após remoção: {ItemId}, Personagem: {CharacterId}",
                id, item.CharacterId);
        }
        
        return result;
    }

    public async Task<InventoryItem> UpdateAsync(InventoryItem item)
    {
        // Atualizar no repositório subjacente
        var updatedItem = await _decorated.UpdateAsync(item);
        
        try
        {
            // Atualizar no cache de item
            var itemKey = GetItemKey(item.Id);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };
            
            await _cache.SetStringAsync(itemKey, JsonSerializer.Serialize(updatedItem), options);
            
            // Invalidar o cache de inventário para forçar atualização em próxima consulta
            await InvalidateInventoryCache(item.CharacterId);
            
            _logger.LogInformation("Cache de item atualizado e cache de inventário invalidado: {ItemId}, Personagem: {CharacterId}",
                item.Id, item.CharacterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar cache após atualização de item: {Message}", ex.Message);
            // Falha silenciosa - retorna os dados mesmo sem cache
        }
        
        return updatedItem;
    }
    
    private async Task InvalidateInventoryCache(Guid characterId)
    {
        await _cache.RemoveAsync(GetInventoryKey(characterId));
        await _cache.RemoveAsync(GetCountKey(characterId));
    }
}