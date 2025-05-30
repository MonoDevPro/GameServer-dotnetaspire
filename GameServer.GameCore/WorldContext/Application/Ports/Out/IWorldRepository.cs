using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using GameServer.WorldService.Domain.Entities;
using GameServer.WorldSimulationService.Domain.Entities;

namespace GameServer.WorldSimulationService.Application.Ports.Out
{
    /// <summary>
    /// Port for persisting world entities and state
    /// </summary>
    public interface IWorldRepository
    {
        // World operations
        Task<World?> GetWorldByIdAsync(long worldId);
        Task<IEnumerable<World>> GetAllWorldsAsync();
        Task<World?> CreateWorldAsync(World world);
        Task<bool> UpdateWorldAsync(World world);
        Task<bool> DeleteWorldAsync(long worldId);
        
        // Region operations
        Task<Region?> GetRegionByIdAsync(long regionId);
        Task<IEnumerable<Region>> GetRegionsByWorldIdAsync(long worldId);
        Task<Region?> CreateRegionAsync(Region region);
        Task<bool> UpdateRegionAsync(Region region);
        Task<bool> DeleteRegionAsync(long regionId);
        
        // Player operations
        Task<Player?> GetPlayerByIdAsync(long playerId);
        Task<Player?> GetPlayerByCharacterIdAsync(Guid characterId);
        Task<IEnumerable<Player>> GetPlayersByRegionIdAsync(long regionId);
        Task<Player?> CreatePlayerAsync(Player player);
        Task<bool> UpdatePlayerAsync(Player player);
        Task<bool> DeletePlayerAsync(long playerId);
        
        // NPC operations
        Task<NPC?> GetNPCByIdAsync(long npcId);
        Task<IEnumerable<NPC>> GetNPCsByRegionIdAsync(long regionId);
        Task<NPC?> CreateNPCAsync(NPC npc);
        Task<bool> UpdateNPCAsync(NPC npc);
        Task<bool> DeleteNPCAsync(long npcId);
        
        // Query operations
        Task<IEnumerable<GameEntity>> GetEntitiesInRadiusAsync(Vector3 center, float radius);
    }
}