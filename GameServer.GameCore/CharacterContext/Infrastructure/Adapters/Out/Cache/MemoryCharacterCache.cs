using System.Collections.Concurrent;
using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Cache;

/// <summary>
/// Opções de configuração para o cache de personagens
/// </summary>
public class CharacterCacheOptions
{
    public const string SectionName = "CharacterCache";
    
    /// <summary>
    /// Tempo de expiração do cache de personagens em minutos
    /// </summary>
    public int CharacterCacheExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// Limite máximo de entradas no cache
    /// </summary>
    public int SizeLimit { get; set; } = 1000;
}

/// <summary>
/// Implementação do serviço de cache de personagens usando MemoryCache
/// </summary>
public class MemoryCharacterCache : ICharacterCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCharacterCache> _logger;
    private readonly MemoryCacheEntryOptions _characterCacheOptions;
    private readonly MemoryCacheEntryOptions _inventoryItemCacheOptions;

    // Prefixos para as chaves do cache para evitar colisões
    private const string CharacterByIdPrefix = "character:id:";
    private const string CharacterByNamePrefix = "character:name:";
    private const string CharacterByUserIdPrefix = "character:userid:";
    private const string InventoryItemByIdPrefix = "inventory:id:";
    private const string InventoryItemsByCharacterIdPrefix = "inventory:characterid:";
    
    public MemoryCharacterCache(
        IMemoryCache cache, 
        ILogger<MemoryCharacterCache> logger, 
        IOptions<CharacterCacheOptions> options)
    {
        _cache = cache;
        _logger = logger;
        
        // Configuração do cache de personagens
        _characterCacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(options.Value.CharacterCacheExpirationMinutes))
            .SetSize(1) // 1 unidade de tamanho por entrada
            .SetPriority(CacheItemPriority.Normal);

        // Configuração do cache de itens de inventário
        _inventoryItemCacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(options.Value.CharacterCacheExpirationMinutes))
            .SetSize(1)
            .SetPriority(CacheItemPriority.Normal);
    }

    public Task<Character?> GetCharacterByIdAsync(Guid id)
    {
        try
        {
            var cacheKey = $"{CharacterByIdPrefix}{id}";
            if (_cache.TryGetValue(cacheKey, out Character? character))
            {
                _logger.LogDebug("Personagem {CharacterId} encontrado no cache", id);
                return Task.FromResult(character);
            }

            _logger.LogDebug("Personagem {CharacterId} não encontrado no cache", id);
            return Task.FromResult<Character?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar personagem {CharacterId} no cache", id);
            return Task.FromResult<Character?>(null);
        }
    }

    public Task<Character?> GetCharacterByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Task.FromResult<Character?>(null);
        
        try
        {
            var cacheKey = $"{CharacterByNamePrefix}{name.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out Guid characterId))
            {
                // Se encontrarmos o ID no cache, buscamos o personagem pelo ID
                return GetCharacterByIdAsync(characterId);
            }

            _logger.LogDebug("Personagem com nome {Name} não encontrado no cache", name);
            return Task.FromResult<Character?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar personagem pelo nome {Name} no cache", name);
            return Task.FromResult<Character?>(null);
        }
    }

    public Task<IEnumerable<Character>?> GetCharactersByUserIdAsync(Guid userId)
    {
        try
        {
            var cacheKey = $"{CharacterByUserIdPrefix}{userId}";
            if (_cache.TryGetValue(cacheKey, out List<Guid>? characterIds) && characterIds != null)
            {
                var tasks = characterIds.Select(GetCharacterByIdAsync);
                var characters = Task.WhenAll(tasks).GetAwaiter().GetResult()
                    .Where(c => c != null)
                    .Cast<Character>();
                    
                _logger.LogDebug("Lista de personagens para usuário {UserId} encontrada no cache", userId);
                return Task.FromResult<IEnumerable<Character>?>(characters);
            }

            _logger.LogDebug("Lista de personagens para usuário {UserId} não encontrada no cache", userId);
            return Task.FromResult<IEnumerable<Character>?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar personagens do usuário {UserId} no cache", userId);
            return Task.FromResult<IEnumerable<Character>?>(null);
        }
    }

    public Task SetCharacterAsync(Character character)
    {
        if (character == null) return Task.CompletedTask;
        
        try
        {
            // Armazenamos o personagem completo usando o ID como chave
            var characterIdKey = $"{CharacterByIdPrefix}{character.Id}";
            _cache.Set(characterIdKey, character, _characterCacheOptions);

            // Também armazenamos índice para o nome
            var nameKey = $"{CharacterByNamePrefix}{character.Name.ToLower()}";
            _cache.Set(nameKey, character.Id, _characterCacheOptions);

            // Atualizamos o índice de personagens por usuário
            var userIdKey = $"{CharacterByUserIdPrefix}{character.AccountId}";
            List<Guid> characterIds;
            
            if (_cache.TryGetValue(userIdKey, out List<Guid>? existingCharacterIds))
            {
                characterIds = existingCharacterIds ?? new List<Guid>();
                if (!characterIds.Contains(character.Id))
                {
                    characterIds.Add(character.Id);
                }
            }
            else
            {
                characterIds = new List<Guid> { character.Id };
            }
            
            _cache.Set(userIdKey, characterIds, _characterCacheOptions);

            _logger.LogDebug("Personagem {CharacterId} armazenado no cache", character.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao armazenar personagem {CharacterId} no cache", character.Id);
        }

        return Task.CompletedTask;
    }

    public Task RemoveCharacterAsync(Guid id)
    {
        try
        {
            // Primeiro obtemos o personagem para remover também as referências de nome e usuário
            if (_cache.TryGetValue($"{CharacterByIdPrefix}{id}", out Character? character) && character != null)
            {
                _cache.Remove($"{CharacterByNamePrefix}{character.Name.ToLower()}");
                
                // Remover do índice de usuários
                var userIdKey = $"{CharacterByUserIdPrefix}{character.AccountId}";
                if (_cache.TryGetValue(userIdKey, out List<Guid>? characterIds) && characterIds != null)
                {
                    characterIds.Remove(id);
                    if (characterIds.Any())
                    {
                        _cache.Set(userIdKey, characterIds, _characterCacheOptions);
                    }
                    else
                    {
                        _cache.Remove(userIdKey);
                    }
                }
                
                // Remover também os itens de inventário associados
                var inventoryKey = $"{InventoryItemsByCharacterIdPrefix}{id}";
                if (_cache.TryGetValue(inventoryKey, out List<Guid>? itemIds) && itemIds != null)
                {
                    foreach (var itemId in itemIds)
                    {
                        _cache.Remove($"{InventoryItemByIdPrefix}{itemId}");
                    }
                    _cache.Remove(inventoryKey);
                }
            }
            
            _cache.Remove($"{CharacterByIdPrefix}{id}");
            _logger.LogDebug("Personagem {CharacterId} removido do cache", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao remover personagem {CharacterId} do cache", id);
        }

        return Task.CompletedTask;
    }

    public Task<InventoryItem?> GetInventoryItemByIdAsync(Guid id)
    {
        try
        {
            var cacheKey = $"{InventoryItemByIdPrefix}{id}";
            if (_cache.TryGetValue(cacheKey, out InventoryItem? item))
            {
                _logger.LogDebug("Item de inventário {ItemId} encontrado no cache", id);
                return Task.FromResult(item);
            }

            _logger.LogDebug("Item de inventário {ItemId} não encontrado no cache", id);
            return Task.FromResult<InventoryItem?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar item de inventário {ItemId} no cache", id);
            return Task.FromResult<InventoryItem?>(null);
        }
    }

    public Task<IEnumerable<InventoryItem>?> GetInventoryItemsByCharacterIdAsync(Guid characterId)
    {
        try
        {
            var cacheKey = $"{InventoryItemsByCharacterIdPrefix}{characterId}";
            if (_cache.TryGetValue(cacheKey, out List<Guid>? itemIds) && itemIds != null)
            {
                var tasks = itemIds.Select(GetInventoryItemByIdAsync);
                var items = Task.WhenAll(tasks).GetAwaiter().GetResult()
                    .Where(i => i != null)
                    .Cast<InventoryItem>();
                    
                _logger.LogDebug("Lista de itens para personagem {CharacterId} encontrada no cache", characterId);
                return Task.FromResult<IEnumerable<InventoryItem>?>(items);
            }

            _logger.LogDebug("Lista de itens para personagem {CharacterId} não encontrada no cache", characterId);
            return Task.FromResult<IEnumerable<InventoryItem>?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar itens do personagem {CharacterId} no cache", characterId);
            return Task.FromResult<IEnumerable<InventoryItem>?>(null);
        }
    }

    public Task SetInventoryItemAsync(InventoryItem item)
    {
        if (item == null) return Task.CompletedTask;
        
        try
        {
            // Armazenamos o item completo usando o ID como chave
            var itemIdKey = $"{InventoryItemByIdPrefix}{item.Id}";
            _cache.Set(itemIdKey, item, _inventoryItemCacheOptions);

            // Atualizamos o índice de itens por personagem
            var characterKey = $"{InventoryItemsByCharacterIdPrefix}{item.CharacterId}";
            List<Guid> itemIds;
            
            if (_cache.TryGetValue(characterKey, out List<Guid>? existingItemIds))
            {
                itemIds = existingItemIds ?? new List<Guid>();
                if (!itemIds.Contains(item.Id))
                {
                    itemIds.Add(item.Id);
                }
            }
            else
            {
                itemIds = new List<Guid> { item.Id };
            }
            
            _cache.Set(characterKey, itemIds, _inventoryItemCacheOptions);

            _logger.LogDebug("Item de inventário {ItemId} armazenado no cache", item.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao armazenar item de inventário {ItemId} no cache", item.Id);
        }

        return Task.CompletedTask;
    }

    public Task RemoveInventoryItemAsync(Guid id)
    {
        try
        {
            // Primeiro obtemos o item para remover também a referência no personagem
            if (_cache.TryGetValue($"{InventoryItemByIdPrefix}{id}", out InventoryItem? item) && item != null)
            {
                // Remover do índice de personagens
                var characterKey = $"{InventoryItemsByCharacterIdPrefix}{item.CharacterId}";
                if (_cache.TryGetValue(characterKey, out List<Guid>? itemIds) && itemIds != null)
                {
                    itemIds.Remove(id);
                    if (itemIds.Any())
                    {
                        _cache.Set(characterKey, itemIds, _inventoryItemCacheOptions);
                    }
                    else
                    {
                        _cache.Remove(characterKey);
                    }
                }
            }
            
            _cache.Remove($"{InventoryItemByIdPrefix}{id}");
            _logger.LogDebug("Item de inventário {ItemId} removido do cache", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao remover item de inventário {ItemId} do cache", id);
        }

        return Task.CompletedTask;
    }
}