using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Persistence.EntityFramework;

/// <summary>
/// Implementação do repositório de itens de inventário usando Entity Framework Core
/// </summary>
public class EfInventoryRepository : IInventoryRepository
{
    private readonly CharacterDbContext _dbContext;
    private readonly ILogger<EfInventoryRepository> _logger;

    public EfInventoryRepository(CharacterDbContext dbContext, ILogger<EfInventoryRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<InventoryItem?> AddAsync(InventoryItem item)
    {
        try
        {
            await _dbContext.InventoryItems.AddAsync(item);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Item adicionado com sucesso ao inventário: {ItemId} para personagem: {CharacterId}", 
                item.Id, item.CharacterId);
                
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar item ao inventário no banco de dados: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<InventoryItem>> GetByCharacterIdAsync(Guid characterId)
    {
        try
        {
            return await _dbContext.InventoryItems
                .Where(i => i.CharacterId == characterId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter itens do inventário para personagem {CharacterId}: {Message}", 
                characterId, ex.Message);
            throw;
        }
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _dbContext.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter item do inventário por ID {ItemId}: {Message}", id, ex.Message);
            throw;
        }
    }

    public async Task<int> GetCountByCharacterIdAsync(Guid characterId)
    {
        try
        {
            return await _dbContext.InventoryItems
                .CountAsync(i => i.CharacterId == characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter contagem de itens do inventário para personagem {CharacterId}: {Message}", 
                characterId, ex.Message);
            throw;
        }
    }

    public async Task<bool> RemoveAsync(Guid id)
    {
        try
        {
            var item = await _dbContext.InventoryItems.FindAsync(id);
            if (item == null)
            {
                _logger.LogWarning("Tentativa de remover item do inventário inexistente: {ItemId}", id);
                return false;
            }

            _dbContext.InventoryItems.Remove(item);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Item removido do inventário com sucesso: {ItemId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover item do inventário {ItemId} do banco de dados: {Message}", id, ex.Message);
            throw;
        }
    }

    public async Task<InventoryItem?> UpdateAsync(InventoryItem item)
    {
        try
        {
            _dbContext.InventoryItems.Update(item);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Item do inventário atualizado com sucesso: {ItemId}", item.Id);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar item do inventário {ItemId} no banco de dados: {Message}", 
                item.Id, ex.Message);
            throw;
        }
    }
}