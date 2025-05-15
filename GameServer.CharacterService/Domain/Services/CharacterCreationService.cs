using GameServer.CharacterService.Domain.Entities;
using GameServer.CharacterService.Domain.ValueObjects;

namespace GameServer.CharacterService.Domain.Services;

/// <summary>
/// Serviço de domínio responsável pela criação de personagens
/// </summary>
public class CharacterCreationService
{
    public Character CreateCharacter(Guid accountId, string name, string characterClass)
    {
        ValidateCharacterData(name, characterClass);
        return new Character(accountId, name, characterClass);
    }

    private void ValidateCharacterData(string name, string characterClass)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do personagem não pode ser vazio", nameof(name));

        if (name.Length < 3 || name.Length > 20)
            throw new ArgumentException("Nome do personagem deve ter entre 3 e 20 caracteres", nameof(name));

        if (string.IsNullOrWhiteSpace(characterClass))
            throw new ArgumentException("Classe do personagem não pode ser vazia", nameof(characterClass));

        // Aqui poderíamos validar se a classe é válida de acordo com as regras do jogo
        var validClasses = new[] { "Guerreiro", "Mago", "Arqueiro", "Curandeiro" };
        if (!validClasses.Contains(characterClass))
            throw new ArgumentException($"Classe de personagem inválida. Classes válidas: {string.Join(", ", validClasses)}", nameof(characterClass));
    }
}