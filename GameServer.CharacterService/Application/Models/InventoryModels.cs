namespace GameServer.CharacterService.Application.Models;

// Request models
public record AddItemRequest(
    string ItemId,
    string Name,
    int Quantity,
    DateTime? ExpiresAt);
    
public record RemoveItemRequest(
    string ItemId,
    int Quantity);
    
public record EquipItemRequest(
    Guid ItemId);
    
public record UnequipItemRequest(
    Guid ItemId);

// Response models
public record InventoryItemResponse(
    Guid Id,
    string ItemId,
    string Name,
    int Quantity,
    bool IsEquipped,
    DateTime AcquiredAt,
    DateTime? ExpiresAt);
    
public record InventoryResponse(
    Guid CharacterId,
    IEnumerable<InventoryItemResponse> Items,
    int TotalItems);