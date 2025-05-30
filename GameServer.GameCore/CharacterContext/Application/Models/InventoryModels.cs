namespace GameServer.GameCore.Character.Application.Models;

// Request models
public record AddItemRequest(
    long ItemId,
    string Name,
    int Quantity = 1,
    DateTime? ExpiresAt = null);
    
public record RemoveItemRequest(
    long ItemId,
    int Quantity = 1);
    
public record EquipItemRequest(
    long ItemId);
    
public record UnequipItemRequest(
    long ItemId);

// Response models
public record InventoryItemResponse(
    long Id,
    long ItemId,
    string Name,
    int Quantity,
    DateTime AcquiredAt,
    DateTime? ExpiresAt);
    
public record InventoryResponse(
    long CharacterId,
    IEnumerable<InventoryItemResponse> Items,
    int TotalItems);