using System;
using System.Collections.Generic;
using GameServer.WorldSimulationService.Domain.Entities;

namespace GameServer.WorldService.Domain.Entities
{
    /// <summary>
    /// Representa o mundo do jogo, contendo informações sobre as regiões, instâncias e estado global
    /// </summary>
    public sealed class World : Entity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime LastUpdated { get; private set; }
        public bool IsActive { get; private set; }
        public List<Region> Regions { get; private set; } = new List<Region>();
        
        public World(string name, string description)
        {
            Name = name;
            Description = description;
            CreatedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            IsActive = true;
        }
        
        public void AddRegion(Region region)
        {
            Regions.Add(region);
            LastUpdated = DateTime.UtcNow;
        }
        
        public void Deactivate()
        {
            IsActive = false;
            LastUpdated = DateTime.UtcNow;
        }
    }
}