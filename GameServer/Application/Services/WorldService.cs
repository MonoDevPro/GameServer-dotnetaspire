using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using GameServer.WorldService.Application.Ports;
using GameServer.WorldService.Domain.Entities;

namespace GameServer.WorldService.Application.Services
{
    /// <summary>
    /// Implementação concreta do serviço que gerencia as operações de alto nível do mundo do jogo
    /// </summary>
    public class WorldService : IWorldService
    {
        private readonly IWorldRepository _worldRepository;

        public WorldService(IWorldRepository worldRepository)
        {
            _worldRepository = worldRepository;
        }

        #region World Operations
        public async Task<World> GetWorldAsync(Guid id)
        {
            return await _worldRepository.GetWorldByIdAsync(id);
        }

        public async Task<IEnumerable<World>> GetAllWorldsAsync()
        {
            return await _worldRepository.GetAllWorldsAsync();
        }

        public async Task<World> CreateWorldAsync(string name, string description)
        {
            var world = new World(name, description);
            await _worldRepository.CreateWorldAsync(world);
            return world;
        }
        #endregion

        #region Region Operations
        public async Task<Region> GetRegionAsync(Guid id)
        {
            return await _worldRepository.GetRegionByIdAsync(id);
        }

        public async Task<IEnumerable<Region>> GetRegionsByWorldAsync(Guid worldId)
        {
            return await _worldRepository.GetRegionsByWorldIdAsync(worldId);
        }

        public async Task<Region> CreateRegionAsync(string name, string description, float width, float height, Vector2 worldPosition, Guid worldId)
        {
            var region = new Region(name, description, width, height, worldPosition);
            await _worldRepository.CreateRegionAsync(region, worldId);
            return region;
        }
        #endregion

        #region Player Operations
        public async Task<Player> GetPlayerAsync(Guid id)
        {
            var entity = await _worldRepository.GetEntityByIdAsync(id);
            return entity as Player;
        }

        public async Task<bool> AddPlayerToRegionAsync(Player player, Guid regionId)
        {
            return await _worldRepository.AddEntityToRegionAsync(player, regionId);
        }

        public async Task<bool> RemovePlayerFromRegionAsync(Guid playerId, Guid regionId)
        {
            return await _worldRepository.RemoveEntityFromRegionAsync(playerId, regionId);
        }

        public async Task<bool> UpdatePlayerPositionAsync(Guid playerId, Vector3 newPosition)
        {
            return await _worldRepository.UpdateEntityPositionAsync(playerId, newPosition);
        }

        public async Task<bool> UpdatePlayerHealthAsync(Guid playerId, int newHealth)
        {
            var player = await GetPlayerAsync(playerId);
            if (player == null)
                return false;

            player.UpdateHealth(newHealth);
            // Aqui precisaríamos de um método específico para atualizar propriedades específicas do jogador
            // Por hora, vamos assumir que a atualização da posição também atualiza outras propriedades
            return true;
        }
        #endregion

        #region NPC Operations
        public async Task<NPC> GetNPCAsync(Guid id)
        {
            var entity = await _worldRepository.GetEntityByIdAsync(id);
            return entity as NPC;
        }

        public async Task<IEnumerable<NPC>> GetNPCsByRegionAsync(Guid regionId)
        {
            var entities = await _worldRepository.GetEntitiesByRegionIdAsync(regionId);
            var npcs = new List<NPC>();
            
            foreach (var entity in entities)
            {
                if (entity is NPC npc)
                {
                    npcs.Add(npc);
                }
            }
            
            return npcs;
        }

        public async Task<NPC> CreateNPCAsync(string name, Vector3 position, Quaternion rotation, Vector3 scale, 
                                            NPCType type, int maxHealth, bool isInteractable, 
                                            string dialogue, Guid regionId)
        {
            var npc = new NPC(name, position, rotation, scale, type, maxHealth, isInteractable, dialogue);
            await _worldRepository.AddEntityToRegionAsync(npc, regionId);
            return npc;
        }

        public async Task<bool> UpdateNPCPositionAsync(Guid npcId, Vector3 newPosition)
        {
            return await _worldRepository.UpdateEntityPositionAsync(npcId, newPosition);
        }
        #endregion
    }
}