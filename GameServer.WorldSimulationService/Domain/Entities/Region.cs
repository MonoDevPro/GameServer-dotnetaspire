using System;
using System.Collections.Generic;
using System.Numerics;
using GameServer.WorldSimulationService.Domain.Entities;

namespace GameServer.WorldService.Domain.Entities
{
    /// <summary>
    /// Representa uma região do mundo, com suas características específicas e entidades presentes
    /// </summary>
    public sealed class Region : Entity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime LastUpdated { get; private set; }
        public bool IsActive { get; private set; }
        public List<GameEntity> Entities { get; private set; } = new List<GameEntity>();
        
        // Coordenadas para posicionar a região no mundo
        public Vector2 WorldPosition { get; private set; }

        // Foreign key to world (optional)
        public long WorldId { get; set; }
        public World World { get; set; } = null!;
        
        public Region(string name, string description, float width, float height, Vector2 worldPosition)
        {
            Name = name;
            Description = description;
            Width = width;
            Height = height;
            WorldPosition = worldPosition;
            CreatedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            IsActive = true;
        }
        
        public void AddEntity(GameEntity entity)
        {
            Entities.Add(entity);
            LastUpdated = DateTime.UtcNow;
        }
        
        public void RemoveEntity(GameEntity entity)
        {
            Entities.Remove(entity);
            LastUpdated = DateTime.UtcNow;
        }
        
        public bool IsPositionWithinBounds(Vector2 position)
        {
            return position.X >= WorldPosition.X && 
                   position.X <= WorldPosition.X + Width &&
                   position.Y >= WorldPosition.Y && 
                   position.Y <= WorldPosition.Y + Height;
        }
        
        public void Deactivate()
        {
            IsActive = false;
            LastUpdated = DateTime.UtcNow;
        }
    }
}