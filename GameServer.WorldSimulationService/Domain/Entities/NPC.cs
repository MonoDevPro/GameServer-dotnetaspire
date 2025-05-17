using System;
using System.Numerics;

namespace GameServer.WorldService.Domain.Entities
{
    public enum NPCType
    {
        Friendly,
        Neutral,
        Hostile
    }

    /// <summary>
    /// Representa um personagem não-jogável (NPC) no mundo do jogo
    /// </summary>
    public class NPC : GameEntity
    {
        public NPCType Type { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsInteractable { get; private set; }
        public string Dialogue { get; private set; }
        
        // Área de patrulha/movimento do NPC
        public Vector3 MovementCenter { get; private set; }
        public float MovementRadius { get; private set; }
        
        // Foreign key to region (optional)
        public long RegionId { get; set; }
        public Region Region { get; set; } = null!;

        public NPC(
            string name,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            NPCType type,
            int maxHealth,
            bool isInteractable,
            string dialogue = "",
            Vector3? movementCenter = null,
            float movementRadius = 0)
            : base(name, position, rotation, scale)
        {
            Type = type;
            MaxHealth = maxHealth;
            Health = maxHealth;
            IsInteractable = isInteractable;
            Dialogue = dialogue;
            MovementCenter = movementCenter ?? position;
            MovementRadius = movementRadius;
        }

        public void UpdateHealth(int newHealth)
        {
            Health = Math.Clamp(newHealth, 0, MaxHealth);
            LastUpdated = DateTime.UtcNow;
            
            if (Health <= 0)
            {
                // Lógica para quando o NPC morre
                Deactivate();
            }
        }
        
        public void SetDialogue(string newDialogue)
        {
            Dialogue = newDialogue;
            LastUpdated = DateTime.UtcNow;
        }
        
        public void TakeDamage(int amount)
        {
            Health = Math.Max(Health - amount, 0);
            LastUpdated = DateTime.UtcNow;
            
            if (Health <= 0)
            {
                // Lógica para quando o NPC morre
                Deactivate();
            }
        }
        
        public void SetMovementArea(Vector3 center, float radius)
        {
            MovementCenter = center;
            MovementRadius = radius;
            LastUpdated = DateTime.UtcNow;
        }
    }
}