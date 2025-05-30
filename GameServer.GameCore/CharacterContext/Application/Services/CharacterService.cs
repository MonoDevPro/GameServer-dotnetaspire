using GameServer.CharacterService.Application.Ports.In;
using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;
using GameServer.CharacterService.Domain.Services;
using GameServer.GameCore.Character.Application.Models;

namespace GameServer.CharacterService.Application.Services;

public class CharacterService : ICharacterUseCases
{
    private readonly ICharacterRepository _characterRepository;
    private readonly CharacterCreationService _characterCreationService;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(
        ICharacterRepository characterRepository,
        CharacterCreationService characterCreationService,
        ILogger<CharacterService> logger)
    {
        _characterRepository = characterRepository;
        _characterCreationService = characterCreationService;
        _logger = logger;
    }

    public async Task<CharacterResponse> CreateCharacter(Guid accountId, CreateCharacterRequest request)
    {
        // Verificar se o nome já está em uso
        var nameExists = await _characterRepository.ExistsByNameAsync(request.Name);
        if (nameExists)
        {
            _logger.LogWarning("Tentativa de criar personagem com nome já existente: {Name}", request.Name);
            throw new ArgumentException("Este nome já está em uso", nameof(request.Name));
        }

        // Verificar quantos personagens a conta já tem
        var characterCount = await _characterRepository.GetCountByUserIdAsync(accountId);
        if (characterCount >= 5) // Limite de 5 personagens por conta
        {
            _logger.LogWarning("Tentativa de criar mais de 5 personagens para conta: {AccountId}", accountId);
            throw new InvalidOperationException("Limite de personagens por conta atingido");
        }

        try
        {
            // Criar o personagem usando o serviço de domínio
            var character = _characterCreationService.CreateCharacter(accountId, request.Name, request.Class);
            
            // Persistir no repositório
            var createdCharacter = await _characterRepository.CreateAsync(character);
            
            _logger.LogInformation("Personagem criado com sucesso: {CharacterId} para conta: {AccountId}", 
                createdCharacter.Id, accountId);
            
            // Mapear para o modelo de resposta
            return MapToCharacterResponse(createdCharacter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar personagem: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<CharacterListResponse> GetCharactersByAccount(Guid accountId)
    {
        try
        {
            var characters = await _characterRepository.GetByUserIdAsync(accountId);
            var totalCount = await _characterRepository.GetCountByUserIdAsync(accountId);
            
            var characterResponses = characters.Select(MapToCharacterResponse);
            
            return new CharacterListResponse(characterResponses, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter personagens da conta {AccountId}: {Message}", accountId, ex.Message);
            throw;
        }
    }

    public async Task<CharacterResponse> GetCharacterById(Guid characterId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            _logger.LogWarning("Personagem não encontrado: {CharacterId}", characterId);
            throw new KeyNotFoundException($"Personagem com ID {characterId} não encontrado");
        }

        return MapToCharacterResponse(character);
    }

    public async Task<CharacterResponse> UpdateCharacter(Guid characterId, UpdateCharacterRequest request)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            _logger.LogWarning("Tentativa de atualizar personagem inexistente: {CharacterId}", characterId);
            throw new KeyNotFoundException($"Personagem com ID {characterId} não encontrado");
        }

        try
        {
            // Atualizar apenas campos não nulos
            string updatedName = request.Name ?? character.Name;
            string updatedClass = request.Class ?? character.Class;
            
            // Se o nome for alterado, verificar se já existe
            if (request.Name != null && request.Name != character.Name)
            {
                var nameExists = await _characterRepository.ExistsByNameAsync(request.Name);
                if (nameExists)
                {
                    _logger.LogWarning("Tentativa de atualizar para nome já existente: {Name}", request.Name);
                    throw new ArgumentException("Este nome já está em uso", nameof(request.Name));
                }
            }
            
            // Atualizar o personagem
            character.UpdateCharacter(updatedName, updatedClass);
            
            // Persistir as alterações
            var updatedCharacter = await _characterRepository.UpdateAsync(character);
            
            _logger.LogInformation("Personagem atualizado com sucesso: {CharacterId}", characterId);
            return MapToCharacterResponse(updatedCharacter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar personagem {CharacterId}: {Message}", characterId, ex.Message);
            throw;
        }
    }

    public async Task<bool> DeactivateCharacter(Guid characterId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            _logger.LogWarning("Tentativa de desativar personagem inexistente: {CharacterId}", characterId);
            throw new KeyNotFoundException($"Personagem com ID {characterId} não encontrado");
        }

        try
        {
            character.Deactivate();
            await _characterRepository.UpdateAsync(character);
            
            _logger.LogInformation("Personagem desativado com sucesso: {CharacterId}", characterId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desativar personagem {CharacterId}: {Message}", characterId, ex.Message);
            return false;
        }
    }

    public async Task<bool> ActivateCharacter(Guid characterId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            _logger.LogWarning("Tentativa de ativar personagem inexistente: {CharacterId}", characterId);
            throw new KeyNotFoundException($"Personagem com ID {characterId} não encontrado");
        }

        try
        {
            character.Activate();
            await _characterRepository.UpdateAsync(character);
            
            _logger.LogInformation("Personagem ativado com sucesso: {CharacterId}", characterId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ativar personagem {CharacterId}: {Message}", characterId, ex.Message);
            return false;
        }
    }

    // Método auxiliar para mapear uma entidade Character para CharacterResponse
    private static CharacterResponse MapToCharacterResponse(Character character)
    {
        return new CharacterResponse(
            character.Id,
            character.Name,
            character.Class,
            character.Level,
            character.CreatedAt,
            character.LastUpdatedAt,
            character.IsActive
        );
    }
}