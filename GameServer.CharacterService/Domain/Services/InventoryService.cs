using GameServer.CharacterService.Domain.Entities;
using GameServer.CharacterService.Domain.ValueObjects;

namespace GameServer.CharacterService.Domain.Services;

/// <summary>
/// Serviço de domínio responsável pela gestão de inventário
/// </summary>
public class InventoryService
{
    public InventoryItem CreateItem(Guid characterId, string itemId, string name, int quantity, DateTime? expiresAt = null)
    {
        ValidateItemData(itemId, name, quantity);
        return new InventoryItem(characterId, itemId, name, quantity, expiresAt);
    }
    
    public bool AddItemToInventory(Character character, InventoryItem item)
    {
        // Verificar se já existe um item igual no inventário
        var existingItem = character.Inventory.FirstOrDefault(i => i.ItemId == item.ItemId && !i.IsEquipped);
        
        if (existingItem != null)
        {
            // Se já existe, apenas aumenta a quantidade
            existingItem.AddQuantity(item.Quantity);
            return true;
        }
        
        // Caso contrário, adiciona o novo item ao inventário
        character.AddItemToInventory(item);
        return true;
    }
    
    public bool RemoveItemFromInventory(Character character, string itemId, int quantity)
    {
        // Buscar todos os itens que correspondem ao ID fornecido
        var items = character.Inventory.Where(i => i.ItemId == itemId && !i.IsEquipped).ToList();
        
        if (!items.Any())
            return false;
        
        int remainingQuantity = quantity;
        
        foreach (var item in items)
        {
            // Se a quantidade do item atual é suficiente para completar a remoção
            if (item.Quantity >= remainingQuantity)
            {
                bool removed = item.RemoveQuantity(remainingQuantity);
                
                // Se o item ficou com quantidade zero, remove do inventário
                if (item.Quantity == 0)
                    character.RemoveItemFromInventory(item);
                
                return removed;
            }
            else
            {
                // Remove toda a quantidade do item atual e continua com o próximo
                remainingQuantity -= item.Quantity;
                character.RemoveItemFromInventory(item);
            }
        }
        
        return remainingQuantity == 0;
    }
    
    public bool EquipItem(Character character, Guid itemId)
    {
        var item = character.Inventory.FirstOrDefault(i => i.Id == itemId);
        
        if (item == null)
            return false;
            
        item.Equip();
        return true;
    }
    
    public bool UnequipItem(Character character, Guid itemId)
    {
        var item = character.Inventory.FirstOrDefault(i => i.Id == itemId);
        
        if (item == null)
            return false;
            
        item.Unequip();
        return true;
    }
    
    public IEnumerable<InventoryItem> GetExpiredItems(IEnumerable<InventoryItem> items)
    {
        return items.Where(i => i.IsExpired());
    }
    
    private void ValidateItemData(string itemId, string name, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("ID do item não pode ser vazio", nameof(itemId));
            
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do item não pode ser vazio", nameof(name));
            
        if (quantity <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero", nameof(quantity));
    }
}