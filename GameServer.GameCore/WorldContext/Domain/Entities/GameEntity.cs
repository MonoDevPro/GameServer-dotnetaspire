using System.Numerics;
using GameServer.WorldService.Domain.Entities;

namespace GameServer.WorldSimulationService.Domain.Entities
{
    /// <summary>
    /// Classe base para todas as entidades do mundo do jogo
    /// </summary>
    public abstract class GameEntity : Entity
    {
        public string Name { get; protected set; }
        public Vector3 Position { get; protected set; }
        public Quaternion Rotation { get; protected set; }
        public Vector3 Scale { get; protected set; }
        public bool IsActive { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime LastUpdated { get; protected set; }
        
        // Foreign key to region (optional)
        public long RegionId { get; set; }
        public virtual Region? Region { get; set; }

        protected GameEntity(
            string name, 
            Vector3 position, 
            Quaternion rotation, 
            Vector3 scale,
            bool isActive = true,
            DateTime createdAt = default,
            DateTime lastUpdated = default,
            
            // Base
            long id = 0
            ) : base(id)
        
        {
            Name = name;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            IsActive = isActive;
            CreatedAt = createdAt == default ? DateTime.UtcNow : createdAt;
            LastUpdated = lastUpdated == default ? DateTime.UtcNow : lastUpdated;
        }

        public virtual void UpdatePosition(Vector3 newPosition)
        {
            Position = newPosition;
            LastUpdated = DateTime.UtcNow;
        }

        public virtual void UpdateRotation(Quaternion newRotation)
        {
            Rotation = newRotation;
            LastUpdated = DateTime.UtcNow;
        }

        public virtual void Activate()
        {
            IsActive = true;
            LastUpdated = DateTime.UtcNow;
        }

        public virtual void Deactivate()
        {
            IsActive = false;
            LastUpdated = DateTime.UtcNow;
        }
    }
}