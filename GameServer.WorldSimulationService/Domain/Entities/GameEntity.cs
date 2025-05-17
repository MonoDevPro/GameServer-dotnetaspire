using System;
using System.Numerics;
using GameServer.WorldSimulationService.Domain.Entities;

namespace GameServer.WorldService.Domain.Entities
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

        protected GameEntity(string name, Vector3 position, Quaternion rotation, Vector3 scale)
        {

            Name = name;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
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