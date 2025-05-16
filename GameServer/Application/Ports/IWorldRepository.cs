using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameServer.WorldService.Domain.Entities;

namespace GameServer.WorldService.Application.Ports
{
    /// <summary>
    /// Interface para o repositório que gerencia a persistência do mundo e suas entidades
    /// </summary>
    public interface IWorldRepository
    {
        Task<World> GetWorldByIdAsync(Guid id);
        Task<IEnumerable<World>> GetAllWorldsAsync();
        Task<bool> CreateWorldAsync(World world);
        Task<bool> UpdateWorldAsync(World world);
        Task<bool> DeleteWorldAsync(Guid id);
        
        Task<Region> GetRegionByIdAsync(Guid id);
        Task<IEnumerable<Region>> GetRegionsByWorldIdAsync(Guid worldId);
        Task<bool> CreateRegionAsync(Region region, Guid worldId);
        Task<bool> UpdateRegionAsync(Region region);
        Task<bool> DeleteRegionAsync(Guid id);
        
        Task<Entity> GetEntityByIdAsync(Guid id);
        Task<IEnumerable<Entity>> GetEntitiesByRegionIdAsync(Guid regionId);
        Task<bool> UpdateEntityPositionAsync(Guid entityId, System.Numerics.Vector3 newPosition);
        Task<bool> AddEntityToRegionAsync(Entity entity, Guid regionId);
        Task<bool> RemoveEntityFromRegionAsync(Guid entityId, Guid regionId);
    }
}