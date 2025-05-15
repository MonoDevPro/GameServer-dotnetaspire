namespace GameServer.CharacterService.Domain.Entities;

/// <summary>
/// Representa um item no inventário de um personagem
/// </summary>
public class InventoryItem
{
    public Guid Id { get; private set; }
    public Guid CharacterId { get; private set; }
    public string ItemId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int Quantity { get; private set; }
    public bool IsEquipped { get; private set; }
    public DateTime AcquiredAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public virtual Character? Character { get; private set; }

    // Construtor privado para EF Core
    private InventoryItem() { }

    public InventoryItem(Guid characterId, string itemId, string name, int quantity, DateTime? expiresAt = null)
    {
        Id = Guid.NewGuid();
        CharacterId = characterId;
        ItemId = itemId;
        Name = name;
        Quantity = quantity;
        IsEquipped = false;
        AcquiredAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public void AddQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero", nameof(amount));
        
        Quantity += amount;
    }

    public bool RemoveQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero", nameof(amount));

        if (Quantity < amount)
            return false;
        
        Quantity -= amount;
        return true;
    }

    public void Equip()
    {
        IsEquipped = true;
    }

    public void Unequip()
    {
        IsEquipped = false;
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }
    
    public void UpdateExpiration(DateTime? newExpiryDate)
    {
        ExpiresAt = newExpiryDate;
    }
}