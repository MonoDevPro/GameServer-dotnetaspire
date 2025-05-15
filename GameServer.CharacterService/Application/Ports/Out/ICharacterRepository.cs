using GameServer.CharacterService.Domain.Entities;

namespace GameServer.CharacterService.Application.Ports.Out;

/// <summary>
/// Porta de saída para acesso ao repositório de personagens
/// </summary>
public interface ICharacterRepository
{
    /// <summary>
    /// Obtém um personagem pelo ID
    /// </summary>
    Task<Character?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Verifica se já existe um personagem com o mesmo nome
    /// </summary>
    Task<bool> ExistsByNameAsync(string name);
    
    /// <summary>
    /// Obtém personagens por ID da conta
    /// </summary>
    Task<IEnumerable<Character>> GetByAccountIdAsync(Guid accountId);
    
    /// <summary>
    /// Obtém a contagem total de personagens por ID da conta
    /// </summary>
    Task<int> GetCountByAccountIdAsync(Guid accountId);
    
    /// <summary>
    /// Cria um novo personagem
    /// </summary>
    Task<Character> CreateAsync(Character character);
    
    /// <summary>
    /// Atualiza um personagem existente
    /// </summary>
    Task<Character> UpdateAsync(Character character);
    
    /// <summary>
    /// Exclui um personagem (exclusão física ou lógica)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}