using System.Numerics;
using GameServer.WorldService.Domain.Entities;
using GameServer.WorldSimulationService.Application.Ports.Out;
using GameServer.WorldSimulationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameServer.WorldSimulationService.Infrastructure.Adapters.Out.Persistence.EntityFramework
{
    public class WorldRepository : IWorldRepository
    {
        private readonly WorldDbContext _dbContext;
        private readonly ILogger<WorldRepository> _logger;

        public WorldRepository(WorldDbContext dbContext, ILogger<WorldRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region World Operations
        public async Task<World?> GetWorldByIdAsync(long worldId)
        {
            try
            {
                var worldEntity = await _dbContext.Worlds
                    .Include(w => w.Regions)
                    .FirstOrDefaultAsync(w => w.Id == worldId);

                return worldEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving world with ID {WorldId}", worldId);
                throw;
            }
        }

        public async Task<IEnumerable<World>> GetAllWorldsAsync()
        {
            try
            {
                var worldEntities = await _dbContext.Worlds
                    .Include(w => w.Regions)
                    .ToListAsync();

                return worldEntities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all worlds");
                throw;
            }
        }

        public async Task<World?> CreateWorldAsync(World worldEntity)
        {
            try
            {
                await _dbContext.Worlds.AddAsync(worldEntity);
                var result = await _dbContext.SaveChangesAsync();

                if (result <= 0)
                    return null;
                
                return await GetWorldByIdAsync(worldEntity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating world {WorldName}", worldEntity.Name);
                throw;
            }
        }

        public async Task<bool> UpdateWorldAsync(World worldEntity)
        {
            try
            {
                var existingWorld = await _dbContext.Worlds
                    .ContainsAsync(worldEntity);

                if (!existingWorld)
                    return false;

                _dbContext.Worlds.Update(worldEntity);

                var result = await _dbContext.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating world with ID {WorldId}", worldEntity.Id);
                throw;
            }
        }

        public async Task<bool> DeleteWorldAsync(long worldId)
        {
            try
            {
                var worldEntity = await _dbContext.Worlds
                    .FirstOrDefaultAsync(w => w.Id == worldId);

                if (worldEntity == null)
                {
                    return false;
                }

                _dbContext.Worlds.Remove(worldEntity);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting world with ID {WorldId}", worldId);
                throw;
            }
        }
        #endregion

        #region Region Operations
        public async Task<Region?> GetRegionByIdAsync(long regionId)
        {
            try
            {
                var regionEntity = await _dbContext.Regions
                    .FirstOrDefaultAsync(r => r.Id == regionId);

                return regionEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving region with ID {RegionId}", regionId);
                throw;
            }
        }

        public async Task<IEnumerable<Region>> GetRegionsByWorldIdAsync(long worldId)
        {
            try
            {
                var regionEntities = await _dbContext.Regions
                    .Where(r => r.WorldId == worldId)
                    .ToListAsync();

                return regionEntities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regions for world {WorldId}", worldId);
                throw;
            }
        }

        public async Task<Region?> CreateRegionAsync(Region regionEntity)
        {
            try
            {
                // For simplicity, we're assuming the region is associated with a world
                var worldId = regionEntity.WorldId; // This is a simplification
                
                var world = await _dbContext.Worlds
                    .FirstOrDefaultAsync(w => w.Id == worldId);
                if (world == null)
                {
                    throw new InvalidOperationException($"World with ID {worldId} does not exist.");
                }

                world.AddRegion(regionEntity);

                var updateWorld = await UpdateWorldAsync(world);

                if (!updateWorld)
                {
                    throw new InvalidOperationException($"Failed to update world with ID {worldId}.");
                }
                
                //var regionEntity = Region.FromDomain(region, worldId);
                //await _dbContext.Regions.AddAsync(regionEntity);
                await _dbContext.SaveChangesAsync();
                
                return await GetRegionByIdAsync(regionEntity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating region {RegionName}", regionEntity.Name);
                throw;
            }
        }

        public async Task<bool> UpdateRegionAsync(Region regionEntity)
        {
            try
            {
                var existingRegion = await _dbContext.Regions
                    .ContainsAsync(regionEntity);

                if (!existingRegion)
                    return false;

                _dbContext.Regions.Update(regionEntity);

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating region with ID {RegionId}", regionEntity.Id);
                throw;
            }
        }

        public async Task<bool> DeleteRegionAsync(long regionId)
        {
            try
            {
                var regionEntity = await _dbContext.Regions
                    .FirstOrDefaultAsync(r => r.Id == regionId);

                if (regionEntity == null)
                {
                    return false;
                }

                _dbContext.Regions.Remove(regionEntity);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting region with ID {RegionId}", regionId);
                throw;
            }
        }
        #endregion

        #region Player Operations
        public async Task<Player?> GetPlayerByIdAsync(long playerId)
        {
            try
            {
                var playerEntity = await _dbContext.Players
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                return playerEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving player with ID {PlayerId}", playerId);
                throw;
            }
        }

        public async Task<Player?> GetPlayerByCharacterIdAsync(Guid characterId)
        {
            try
            {
                var playerEntity = await _dbContext.Players
                    .FirstOrDefaultAsync(p => p.CharacterId == characterId);

                return playerEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving player with character ID {CharacterId}", characterId);
                throw;
            }
        }

        public async Task<IEnumerable<Player>> GetPlayersByRegionIdAsync(long regionId)
        {
            try
            {
                var playerEntities = await _dbContext.Players
                    .Where(p => p.RegionId == regionId)
                    .ToListAsync();

                return playerEntities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving players for region {RegionId}", regionId);
                throw;
            }
        }

        public async Task<Player?> CreatePlayerAsync(Player playerEntity)
        {
            try
            {
                await _dbContext.Players.AddAsync(playerEntity);
                await _dbContext.SaveChangesAsync();
                
                return await GetPlayerByIdAsync(playerEntity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating player {PlayerName}", playerEntity.Name);
                throw;
            }
        }

        public async Task<bool> UpdatePlayerAsync(Player player)
        {
            try
            {
                var existingPlayer = await _dbContext.Players
                    .ContainsAsync(player);

                if (!existingPlayer)
                    return false;

                _dbContext.Update(player);
                var result = await _dbContext.SaveChangesAsync();

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating player with ID {PlayerId}", player.Id);
                throw;
            }
        }

        public async Task<bool> DeletePlayerAsync(long playerId)
        {
            try
            {
                var playerEntity = await _dbContext.Players
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                if (playerEntity == null)
                {
                    return false;
                }

                _dbContext.Players.Remove(playerEntity);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting player with ID {PlayerId}", playerId);
                throw;
            }
        }
        #endregion

        #region NPC Operations
        public async Task<NPC?> GetNPCByIdAsync(long npcId)
        {
            try
            {
                var npcEntity = await _dbContext.NPCs
                    .FirstOrDefaultAsync(n => n.Id == npcId);

                return npcEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving NPC with ID {NpcId}", npcId);
                throw;
            }
        }

        public async Task<IEnumerable<NPC>> GetNPCsByRegionIdAsync(long regionId)
        {
            try
            {
                var npcEntities = await _dbContext.NPCs
                    .Where(n => n.RegionId == regionId)
                    .ToListAsync();

                return npcEntities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving NPCs for region {RegionId}", regionId);
                throw;
            }
        }

        public async Task<NPC?> CreateNPCAsync(NPC npcEntity)
        {
            try
            {
                await _dbContext.NPCs.AddAsync(npcEntity);
                var result = await _dbContext.SaveChangesAsync();

                if (result > 0)
                    return await GetNPCByIdAsync(npcEntity.Id);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating NPC {NpcName}", npcEntity.Name);
                throw;
            }
        }

        public async Task<bool> UpdateNPCAsync(NPC npcEntity)
        {
            try
            {
                var existingNPC = await _dbContext.NPCs
                    .ContainsAsync(npcEntity);

                if (!existingNPC)
                    return false;

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating NPC with ID {NpcId}", npcEntity.Id);
                throw;
            }
        }

        public async Task<bool> DeleteNPCAsync(long npcId)
        {
            try
            {
                var npcEntity = await _dbContext.NPCs
                    .FirstOrDefaultAsync(n => n.Id == npcId);

                if (npcEntity == null)
                {
                    return false;
                }

                _dbContext.NPCs.Remove(npcEntity);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting NPC with ID {NpcId}", npcId);
                throw;
            }
        }
        #endregion

        #region Query Operations
        public async Task<IEnumerable<GameEntity>> GetEntitiesInRadiusAsync(Vector3 center, float radius)
        {
            try
            {
                // This is a simplified implementation that isn't efficient for a real game
                // In a real game, you would use spatial queries or dedicated spatial databases
                
                var entities = new List<GameEntity>();
                
                // Get all players
                var players = await _dbContext.Players.ToListAsync();
                foreach (var player in players)
                {
                    var distance = Vector3.Distance(center, player.Position);
                    if (distance <= radius)
                    {
                        entities.Add(player);
                    }
                }
                
                // Get all NPCs
                var npcs = await _dbContext.NPCs.ToListAsync();
                foreach (var npc in npcs)
                {
                    var distance = Vector3.Distance(center, npc.Position);
                    if (distance <= radius)
                    {
                        entities.Add(npc);
                    }
                }
                
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entities in radius");
                throw;
            }
        }
        #endregion
    }
}