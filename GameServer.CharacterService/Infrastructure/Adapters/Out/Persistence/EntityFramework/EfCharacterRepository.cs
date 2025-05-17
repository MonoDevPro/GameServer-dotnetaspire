using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Persistence.EntityFramework;

/// <summary>
/// Implementação do repositório de personagens usando Entity Framework Core
/// </summary>
public class EfCharacterRepository : ICharacterRepository
{
    private readonly CharacterDbContext _dbContext;
    private readonly ILogger<EfCharacterRepository> _logger;

    public EfCharacterRepository(CharacterDbContext dbContext, ILogger<EfCharacterRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Character?> CreateAsync(Character character)
    {
        try
        {
            await _dbContext.Characters.AddAsync(character);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Personagem criado com sucesso: {CharacterId}", character.Id);
            return character;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar personagem no banco de dados: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var character = await _dbContext.Characters.FindAsync(id);
            if (character == null)
            {
                _logger.LogWarning("Tentativa de excluir personagem inexistente: {CharacterId}", id);
                return false;
            }

            // Exclusão lógica
            character.Deactivate();
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Personagem desativado com sucesso: {CharacterId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir personagem {CharacterId} do banco de dados: {Message}", id, ex.Message);
            throw;
        }
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        try
        {
            return await _dbContext.Characters.AnyAsync(c => c.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar existência de personagem por nome: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<Character?> GetByNameAsync(string name)
    {
        try
        {
            return await _dbContext.Characters.FirstOrDefaultAsync(c => c.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter personagem por nome {CharacterName}: {Message}", name, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<Character>> GetByUserIdAsync(Guid accountId)
    {
        try
        {
            return await _dbContext.Characters
                .Where(c => c.AccountId == accountId)
                .Include(c => c.Inventory)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter personagens por AccountId {AccountId}: {Message}", accountId, ex.Message);
            throw;
        }
    }

    public async Task<Character?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _dbContext.Characters
                .Include(c => c.Inventory)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter personagem por ID {CharacterId}: {Message}", id, ex.Message);
            throw;
        }
    }

    public async Task<int> GetCountByUserIdAsync(Guid accountId)
    {
        try
        {
            return await _dbContext.Characters
                .CountAsync(c => c.AccountId == accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter contagem de personagens por AccountId {AccountId}: {Message}", accountId, ex.Message);
            throw;
        }
    }

    public async Task<Character?> UpdateAsync(Character character)
    {
        try
        {
            _dbContext.Characters.Update(character);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Personagem atualizado com sucesso: {CharacterId}", character.Id);
            return character;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar personagem {CharacterId} no banco de dados: {Message}", character.Id, ex.Message);
            throw;
        }
    }
}