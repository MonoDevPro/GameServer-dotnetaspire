using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Persistence;

/// <summary>
/// Implementação de repositório de personagens que utiliza cache
/// </summary>
public class CachedCharacterRepository : ICharacterRepository
{
    private readonly ICharacterRepository _repository;
    private readonly ICharacterCache _cache;
    private readonly ILogger<CachedCharacterRepository> _logger;

    public CachedCharacterRepository(
        ICharacterRepository repository,
        ICharacterCache cache,
        ILogger<CachedCharacterRepository> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Character?> GetByIdAsync(Guid id)
    {
        // Tenta obter do cache primeiro
        var character = await _cache.GetCharacterByIdAsync(id);
        if (character != null)
        {
            _logger.LogDebug("Personagem {CharacterId} obtido do cache", id);
            return character;
        }

        // Se não estiver no cache, busca do repositório
        character = await _repository.GetByIdAsync(id);

        // Se encontrado, armazena no cache para consultas futuras
        if (character != null)
        {
            await _cache.SetCharacterAsync(character);
            _logger.LogDebug("Personagem {CharacterId} armazenado no cache após busca no repositório", id);
        }

        return character;
    }

    public async Task<int> GetCountByUserIdAsync(Guid accountId)
    {
        // Tenta obter do cache primeiro
        var count = await _cache.GetCharactersByUserIdAsync(accountId);
        if (count != null)
        {
            _logger.LogDebug("Contagem de personagens para a conta {AccountId} obtida do cache", accountId);
            return 0;
        }

        var countResult = await _repository.GetCountByUserIdAsync(accountId);

        // Armazena no cache para consultas futuras
        _logger.LogDebug($"Contagem de personagens para a conta {countResult} armazenada no cache após busca no repositório",
            accountId);

        return countResult;
    }

    public async Task<Character?> GetByNameAsync(string name)
    {
        // Tenta obter do cache primeiro
        var character = await _cache.GetCharacterByNameAsync(name);
        if (character != null)
        {
            _logger.LogDebug("Personagem com nome {Name} obtido do cache", name);
            return character;
        }

        // Se não estiver no cache, busca do repositório
        character = await _repository.GetByNameAsync(name);

        // Se encontrado, armazena no cache para consultas futuras
        if (character != null)
        {
            await _cache.SetCharacterAsync(character);
            _logger.LogDebug("Personagem com nome {Name} armazenado no cache após busca no repositório", name);
        }

        return character;
    }

    public async Task<IEnumerable<Character>> GetByUserIdAsync(Guid userId)
    {
        // Tenta obter do cache primeiro
        var characters = await _cache.GetCharactersByUserIdAsync(userId);
        if (characters != null)
        {
            _logger.LogDebug("Personagens do usuário {UserId} obtidos do cache", userId);
            return characters;
        }

        // Se não estiver no cache, busca do repositório
        characters = await _repository.GetByUserIdAsync(userId);

        // Armazena cada personagem no cache para consultas futuras
        foreach (var character in characters)
        {
            await _cache.SetCharacterAsync(character);
        }

        _logger.LogDebug("{Count} personagens do usuário {UserId} armazenados no cache após busca no repositório",
            characters.Count(), userId);

        return characters;
    }

    public async Task<Character> CreateAsync(Character character)
    {
        // Cria o personagem no repositório
        var createdCharacter = await _repository.CreateAsync(character);

        // Armazena no cache
        await _cache.SetCharacterAsync(createdCharacter);
        _logger.LogDebug("Personagem {CharacterId} criado e armazenado no cache", createdCharacter.Id);

        return createdCharacter;
    }

    public async Task<bool> UpdateAsync(Character character)
    {
        // Atualiza o personagem no repositório
        var updated = await _repository.UpdateAsync(character);

        // Se foi atualizado com sucesso, atualiza também no cache
        if (updated)
        {
            await _cache.SetCharacterAsync(character);
            _logger.LogDebug("Personagem {CharacterId} atualizado no cache", character.Id);
        }

        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        // Remove do repositório
        var deleted = await _repository.DeleteAsync(id);

        // Se foi deletado com sucesso, remove também do cache
        if (deleted)
        {
            await _cache.RemoveCharacterAsync(id);
            _logger.LogDebug("Personagem {CharacterId} removido do cache", id);
        }

        return deleted;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        // Tentamos verificar pelo cache primeiro
        var character = await _cache.GetCharacterByNameAsync(name);
        if (character != null)
        {
            return true;
        }

        // Para verificações de existência, vamos ao repositório para garantir a resposta mais precisa
        return await _repository.ExistsByNameAsync(name);
    }
}