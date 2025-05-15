using GameServer.CharacterService.Application.Models;

namespace GameServer.CharacterService.Application.Ports.In;

/// <summary>
/// Porta de entrada para operações relacionadas a personagens
/// </summary>
public interface ICharacterUseCases
{
    /// <summary>
    /// Cria um novo personagem para a conta especificada
    /// </summary>
    Task<CharacterResponse> CreateCharacter(Guid accountId, CreateCharacterRequest request);
    
    /// <summary>
    /// Obtém todos os personagens de uma conta
    /// </summary>
    Task<CharacterListResponse> GetCharactersByAccount(Guid accountId);
    
    /// <summary>
    /// Obtém um personagem específico pelo ID
    /// </summary>
    Task<CharacterResponse> GetCharacterById(Guid characterId);
    
    /// <summary>
    /// Atualiza um personagem
    /// </summary>
    Task<CharacterResponse> UpdateCharacter(Guid characterId, UpdateCharacterRequest request);
    
    /// <summary>
    /// Desativa um personagem
    /// </summary>
    Task<bool> DeactivateCharacter(Guid characterId);
    
    /// <summary>
    /// Ativa um personagem
    /// </summary>
    Task<bool> ActivateCharacter(Guid characterId);
}