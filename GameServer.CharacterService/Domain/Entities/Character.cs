namespace GameServer.CharacterService.Domain.Entities;

/// <summary>
/// Representa um personagem no jogo
/// </summary>
public class Character
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Class { get; private set; } = null!;
    public int Level { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public virtual List<InventoryItem> Inventory { get; private set; } = [];

    // Construtor privado para EF Core
    private Character() { }

    public Character(Guid accountId, string name, string characterClass)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Name = name;
        Class = characterClass;
        Level = 1;
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void UpdateCharacter(string name, string characterClass)
    {
        Name = name;
        Class = characterClass;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void LevelUp()
    {
        Level++;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void AddItemToInventory(InventoryItem item)
    {
        Inventory.Add(item);
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItemFromInventory(InventoryItem item)
    {
        Inventory.Remove(item);
        LastUpdatedAt = DateTime.UtcNow;
    }
}