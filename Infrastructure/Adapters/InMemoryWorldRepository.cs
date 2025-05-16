using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using GameServer.WorldService.Application.Ports;
using GameServer.WorldService.Domain.Entities;

namespace GameServer.WorldService.Infrastructure.Adapters
{
    /// <summary>
    /// Implementação em memória do repositório de mundo para desenvolvimento e testes
    /// </summary>
    public class InMemoryWorldRepository : IWorldRepository
    {
        private readonly List<World> _worlds = new();
        private readonly Dictionary<Guid, Region> _regions = new();
        private readonly Dictionary<Guid, Entity> _entities = new();
        private readonly Dictionary<Guid, List<Region>> _worldRegions = new();
        private readonly Dictionary<Guid, List<Entity>> _regionEntities = new();

        public Task<World> GetWorldByIdAsync(Guid id)
        {
            var world = _worlds.FirstOrDefault(w => w.Id == id);
            return Task.FromResult(world);
        }

        public Task<IEnumerable<World>> GetAllWorldsAsync()
        {
            return Task.FromResult(_worlds.AsEnumerable());
        }

        public Task<bool> CreateWorldAsync(World world)
        {
            _worlds.Add(world);
            _worldRegions[world.Id] = new List<Region>();
            return Task.FromResult(true);
        }

        public Task<bool> UpdateWorldAsync(World world)
        {
            var existingWorld = _worlds.FirstOrDefault(w => w.Id == world.Id);
            if (existingWorld == null)
                return Task.FromResult(false);

            // Em uma implementação real, atualizaríamos os campos do mundo existente
            return Task.FromResult(true);
        }

        public Task<bool> DeleteWorldAsync(Guid id)
        {
            var world = _worlds.FirstOrDefault(w => w.Id == id);
            if (world == null)
                return Task.FromResult(false);

            _worlds.Remove(world);
            return Task.FromResult(true);
        }

        public Task<Region> GetRegionByIdAsync(Guid id)
        {
            _regions.TryGetValue(id, out var region);
            return Task.FromResult(region);
        }

        public Task<IEnumerable<Region>> GetRegionsByWorldIdAsync(Guid worldId)
        {
            if (!_worldRegions.TryGetValue(worldId, out var regions))
                return Task.FromResult(Enumerable.Empty<Region>());

            return Task.FromResult(regions.AsEnumerable());
        }

        public Task<bool> CreateRegionAsync(Region region, Guid worldId)
        {
            var world = _worlds.FirstOrDefault(w => w.Id == worldId);
            if (world == null)
                return Task.FromResult(false);

            _regions[region.Id] = region;
            if (!_worldRegions.TryGetValue(worldId, out var regions))
            {
                regions = new List<Region>();
                _worldRegions[worldId] = regions;
            }
            regions.Add(region);
            _regionEntities[region.Id] = new List<Entity>();

            return Task.FromResult(true);
        }

        public Task<bool> UpdateRegionAsync(Region region)
        {
            if (!_regions.ContainsKey(region.Id))
                return Task.FromResult(false);

            _regions[region.Id] = region;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteRegionAsync(Guid id)
        {
            if (!_regions.TryGetValue(id, out var region))
                return Task.FromResult(false);

            _regions.Remove(id);
            foreach (var kvp in _worldRegions)
            {
                kvp.Value.Remove(region);
            }
            _regionEntities.Remove(id);
            return Task.FromResult(true);
        }

        public Task<Entity> GetEntityByIdAsync(Guid id)
        {
            _entities.TryGetValue(id, out var entity);
            return Task.FromResult(entity);
        }

        public Task<IEnumerable<Entity>> GetEntitiesByRegionIdAsync(Guid regionId)
        {
            if (!_regionEntities.TryGetValue(regionId, out var entities))
                return Task.FromResult(Enumerable.Empty<Entity>());

            return Task.FromResult(entities.AsEnumerable());
        }

        public Task<bool> UpdateEntityPositionAsync(Guid entityId, Vector3 newPosition)
        {
            if (!_entities.TryGetValue(entityId, out var entity))
                return Task.FromResult(false);

            entity.UpdatePosition(newPosition);
            return Task.FromResult(true);
        }

        public Task<bool> AddEntityToRegionAsync(Entity entity, Guid regionId)
        {
            if (!_regions.ContainsKey(regionId))
                return Task.FromResult(false);

            _entities[entity.Id] = entity;
            if (!_regionEntities.TryGetValue(regionId, out var entities))
            {
                entities = new List<Entity>();
                _regionEntities[regionId] = entities;
            }
            entities.Add(entity);
            return Task.FromResult(true);
        }

        public Task<bool> RemoveEntityFromRegionAsync(Guid entityId, Guid regionId)
        {
            if (!_regionEntities.TryGetValue(regionId, out var entities))
                return Task.FromResult(false);

            var entity = entities.FirstOrDefault(e => e.Id == entityId);
            if (entity == null)
                return Task.FromResult(false);

            entities.Remove(entity);
            return Task.FromResult(true);
        }
    }
}