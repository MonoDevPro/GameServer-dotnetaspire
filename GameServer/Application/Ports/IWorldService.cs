using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using GameServer.WorldService.Domain.Entities;

namespace GameServer.WorldService.Application.Ports
{
    /// <summary>
    /// Interface para o serviço que gerencia as operações de alto nível do mundo do jogo
    /// </summary>
    public interface IWorldService
    {
        // Operações de mundo
        Task<World> GetWorldAsync(Guid id);
        Task<IEnumerable<World>> GetAllWorldsAsync();
        Task<World> CreateWorldAsync(string name, string description);
        
        // Operações de região
        Task<Region> GetRegionAsync(Guid id);
        Task<IEnumerable<Region>> GetRegionsByWorldAsync(Guid worldId);
        Task<Region> CreateRegionAsync(string name, string description, float width, float height, Vector2 worldPosition, Guid worldId);
        
        // Operações de jogadores
        Task<Player> GetPlayerAsync(Guid id);
        Task<bool> AddPlayerToRegionAsync(Player player, Guid regionId);
        Task<bool> RemovePlayerFromRegionAsync(Guid playerId, Guid regionId);
        Task<bool> UpdatePlayerPositionAsync(Guid playerId, Vector3 newPosition);
        Task<bool> UpdatePlayerHealthAsync(Guid playerId, int newHealth);
        
        // Operações de NPCs
        Task<NPC> GetNPCAsync(Guid id);
        Task<IEnumerable<NPC>> GetNPCsByRegionAsync(Guid regionId);
        Task<NPC> CreateNPCAsync(string name, Vector3 position, Quaternion rotation, Vector3 scale, NPCType type, 
                                int maxHealth, bool isInteractable, string dialogue, Guid regionId);
        Task<bool> UpdateNPCPositionAsync(Guid npcId, Vector3 newPosition);
    }
}