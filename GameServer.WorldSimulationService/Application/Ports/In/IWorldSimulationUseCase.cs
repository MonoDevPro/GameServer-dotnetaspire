using System.Numerics;
using GameServer.WorldService.Domain.Entities;
using GameServer.WorldSimulationService.Domain.Entities;

namespace GameServer.WorldSimulationService.Application.Ports.In
{
    /// <summary>
    /// Port for performing world simulation operations
    /// </summary>
    public interface IWorldSimulationUseCase
    {
        // Entity management
        Task<Player?> AddPlayerToWorldAsync(Guid characterId, Guid accountId, string username, string name, Vector3 position);
        Task<bool> RemovePlayerFromWorldAsync(long playerId);
        Task<bool> UpdatePlayerPositionAsync(long playerId, Vector3 newPosition);
        Task<bool> UpdatePlayerRotationAsync(long playerId, Quaternion newRotation);
        Task<Player?> GetPlayerByIdAsync(long playerId);
        
        // NPC management
        Task<NPC?> AddNPCToWorldAsync(string name, Vector3 position, NPCType type, int maxHealth, bool isInteractable, string dialogue);
        Task<bool> UpdateNPCPositionAsync(long npcId, Vector3 newPosition);
        Task<NPC?> GetNPCByIdAsync(long npcId);
        
        // World queries
        Task<IEnumerable<Entity>> GetEntitiesInRadiusAsync(Vector3 center, float radius);
        Task<IEnumerable<Player>> GetPlayersInRegionAsync(long regionId);
        Task<IEnumerable<NPC>> GetNPCsInRegionAsync(long regionId);
        
        // Interactions
        Task<bool> ProcessInteractionAsync(long initiatorId, long targetId, string interactionType);
        
        // World management
        Task<World?> CreateNewWorldAsync(string name, string description);
        Task<World?> GetWorldByIdAsync(long worldId);
        Task<Region?> GetRegionByIdAsync(long regionId);
        Task<Region?> AddRegionToWorldAsync(long worldId, string name, string description, float width, float height, Vector2 worldPosition);
    }
}