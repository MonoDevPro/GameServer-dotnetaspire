using GameServer.CharacterService.Application.Models;
using GameServer.CharacterService.Application.Ports.In;
using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;
using DomainInventoryService = GameServer.CharacterService.Domain.Services.InventoryService;

namespace GameServer.CharacterService.Application.Services;

public class InventoryService : IInventoryUseCases
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly DomainInventoryService _domainInventoryService;
    private readonly IAccountsCache _accountsCache;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        ICharacterRepository characterRepository,
        IInventoryRepository inventoryRepository,
        DomainInventoryService domainInventoryService,
        IAccountsCache accountsCache,
        ILogger<InventoryService> logger)
    {
        _characterRepository = characterRepository;
        _inventoryRepository = inventoryRepository;
        _domainInventoryService = domainInventoryService;
        _accountsCache = accountsCache;
        _logger = logger;
    }

    public async Task<InventoryResponse> GetInventory(Guid characterId)
    {
        // Verificar se o personagem existe
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            _logger.LogWarning("Tentativa de acessar inventário de personagem inexistente: {CharacterId}", characterId);
            throw new KeyNotFoundException($"Personagem com ID {characterId} não encontrado");
        }

        try
        {
            // Obter todos os itens do inventário
            var items = await _inventoryRepository.GetByCharacterIdAsync(characterId);
            var totalItems = await _inventoryRepository.GetCountByCharacterIdAsync(characterId);

            // Remover itens expirados da lista
            var validItems = items.Where(i => !i.IsExpired()).ToList();

            // Mapear para o modelo de resposta
            var itemResponses = validItems.Select(MapToInventoryItemResponse);

            _logger.LogInformation("Inventário obtido com sucesso para personagem: {CharacterId}, Total de itens: {TotalItems}", 
                characterId, validItems.Count);

            return new InventoryResponse(characterId, itemResponses, validItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter inventário para personagem {CharacterId}: {Message}", characterId, ex.Message);
            throw;
        }
    }

    public async Task<InventoryItemResponse> AddItem(Guid characterId, AddItemRequest request)
    {
        // Verificar se o personagem existe e está ativo
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            _logger.LogWarning("Tentativa de adicionar item para personagem inexistente: {CharacterId}", characterId);
            throw new KeyNotFoundException($"Personagem com ID {characterId} não encontrado");
        }

        if (!character.IsActive)
        {
            _logger.LogWarning("Tentativa de adicionar item para personagem inativo: {CharacterId}", characterId);
            throw new InvalidOperationException("Não é possível adicionar itens a um personagem inativo");
        }

        // Verificar se a conta está ativa
        var isActive = await _accountsCache.IsActiveAsync(character.AccountId);
        if (!isActive)
        {
            _logger.LogWarning("Tentativa de adicionar item para personagem de conta inativa: {AccountId}", character.AccountId);
            throw new InvalidOperationException("A conta associada a este personagem não está ativa");
        }

        try
        {
            // Criar o item usando o serviço de domínio
            var item = _domainInventoryService.CreateItem(
                characterId, 
                request.ItemId, 
                request.Name, 
                request.Quantity, 
                request.ExpiresAt
            );

            // Verificar se já existe um item igual no inventário
            var existingItems = await _inventoryRepository.GetByCharacterIdAsync(characterId);
            var existingItem = existingItems.FirstOrDefault(i => 
                i.ItemId == request.ItemId && !i.IsEquipped && (request.ExpiresAt == null || i.ExpiresAt == request.ExpiresAt));

            if (existingItem != null)
            {
                // Adicionar à quantidade existente
                existingItem.AddQuantity(request.Quantity);
                var updatedItem = await _inventoryRepository.UpdateAsync(existingItem);
                
                _logger.LogInformation("Quantidade aumentada para item existente: {ItemId}, Personagem: {CharacterId}", 
                    existingItem.Id, characterId);
                    
                return MapToInventoryItemResponse(updatedItem);
            }
            else
            {
                // Adicionar como novo item
                var addedItem = await _inventoryRepository.AddAsync(item);
                
                // Também adicionar à coleção de inventário do personagem
                character.AddItemToInventory(addedItem);
                await _characterRepository.UpdateAsync(character);
                
                _logger.LogInformation("Novo item adicionado ao inventário: {ItemId}, Personagem: {CharacterId}", 
                    addedItem.Id, characterId);
                    
                return MapToInventoryItemResponse(addedItem);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar item ao inventário do personagem {CharacterId}: {Message}", 
                characterId, ex.Message);
            throw;
        }
    }

    public async Task<bool> RemoveItem(Guid characterId, RemoveItemRequest request)
    {
        // Verificar se o personagem existe
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            _logger.LogWarning("Tentativa de remover item de personagem inexistente: {CharacterId}", characterId);
            throw new KeyNotFoundException($"Personagem com ID {characterId} não encontrado");
        }

        try
        {
            // Usar o serviço de domínio para remover o item
            bool success = _domainInventoryService.RemoveItemFromInventory(character, request.ItemId, request.Quantity);
            
            if (success)
            {
                // Atualizar o personagem no repositório
                await _characterRepository.UpdateAsync(character);
                _logger.LogInformation("Item removido com sucesso: {ItemId}, Quantidade: {Quantity}, Personagem: {CharacterId}", 
                    request.ItemId, request.Quantity, characterId);
            }
            else
            {
                _logger.LogWarning("Falha ao remover item: {ItemId}, Quantidade: {Quantity}, Personagem: {CharacterId}", 
                    request.ItemId, request.Quantity, characterId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover item {ItemId} do personagem {CharacterId}: {Message}", 
                request.ItemId, characterId, ex.Message);
            throw;
        }
    }

    public async Task<bool> EquipItem(Guid characterId, EquipItemRequest request)
    {
        // Verificar se o personagem existe e está ativo
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            _logger.LogWarning("Tentativa de equipar item para personagem inexistente: {CharacterId}", characterId);
            throw new KeyNotFoundException($"Personagem com ID {characterId} não encontrado");
        }

        if (!character.IsActive)
        {
            _logger.LogWarning("Tentativa de equipar item para personagem inativo: {CharacterId}", characterId);
            throw new InvalidOperationException("Não é possível equipar itens em um personagem inativo");
        }

        try
        {
            // Equipar o item usando o serviço de domínio
            bool success = _domainInventoryService.EquipItem(character, request.ItemId);
            
            if (success)
            {
                // Atualizar o personagem no repositório
                await _characterRepository.UpdateAsync(character);
                _logger.LogInformation("Item equipado com sucesso: {ItemId}, Personagem: {CharacterId}", 
                    request.ItemId, characterId);
            }
            else
            {
                _logger.LogWarning("Falha ao equipar item: {ItemId}, Personagem: {CharacterId}", 
                    request.ItemId, characterId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao equipar item {ItemId} do personagem {CharacterId}: {Message}", 
                request.ItemId, characterId, ex.Message);
            throw;
        }
    }

    public async Task<bool> UnequipItem(Guid characterId, UnequipItemRequest request)
    {
        // Verificar se o personagem existe
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            _logger.LogWarning("Tentativa de desequipar item para personagem inexistente: {CharacterId}", characterId);
            throw new KeyNotFoundException($"Personagem com ID {characterId} não encontrado");
        }

        try
        {
            // Desequipar o item usando o serviço de domínio
            bool success = _domainInventoryService.UnequipItem(character, request.ItemId);
            
            if (success)
            {
                // Atualizar o personagem no repositório
                await _characterRepository.UpdateAsync(character);
                _logger.LogInformation("Item desequipado com sucesso: {ItemId}, Personagem: {CharacterId}", 
                    request.ItemId, characterId);
            }
            else
            {
                _logger.LogWarning("Falha ao desequipar item: {ItemId}, Personagem: {CharacterId}", 
                    request.ItemId, characterId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desequipar item {ItemId} do personagem {CharacterId}: {Message}", 
                request.ItemId, characterId, ex.Message);
            throw;
        }
    }

    // Método auxiliar para mapear uma entidade InventoryItem para InventoryItemResponse
    private static InventoryItemResponse MapToInventoryItemResponse(InventoryItem item)
    {
        return new InventoryItemResponse(
            item.Id,
            item.ItemId,
            item.Name,
            item.Quantity,
            item.IsEquipped,
            item.AcquiredAt,
            item.ExpiresAt
        );
    }
}