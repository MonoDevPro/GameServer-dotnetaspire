using System.Numerics;
using GameServer.WorldService.Domain.Entities;

namespace GameServer.WorldSimulationService.Domain.Entities
{
    /// <summary>
    /// Representa um jogador no mundo do jogo
    /// </summary>
    public class Player : GameEntity
    {
        public Guid AccountId { get; private set; }
        public Guid CharacterId { get; private set; }
        public string Username { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public DateTime LastLogin { get; private set; }
        public bool IsOnline { get; private set; }

        public Player(
            Guid characterId,
            Guid accountId,
            string username,
            string name,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            int maxHealth)
            : base(name, position, rotation, scale)
        {
            CharacterId = characterId;
            AccountId = accountId;
            Username = username;
            Health = maxHealth;
            MaxHealth = maxHealth;
            IsActive = true;
            LastUpdated = DateTime.UtcNow;
        }

        public void UpdateHealth(int newHealth)
        {
            Health = Math.Clamp(newHealth, 0, MaxHealth);
            LastUpdated = DateTime.UtcNow;
            
            if (Health <= 0)
            {
                // Lógica para quando o jogador morre
            }
        }

        public void SetOnlineStatus(bool isOnline)
        {
            IsOnline = isOnline;
            if (isOnline)
            {
                LastLogin = DateTime.UtcNow;
            }
            LastUpdated = DateTime.UtcNow;
        }
        
        public void Heal(int amount)
        {
            Health = Math.Min(Health + amount, MaxHealth);
            LastUpdated = DateTime.UtcNow;
        }
        
        public void TakeDamage(int amount)
        {
            Health = Math.Max(Health - amount, 0);
            LastUpdated = DateTime.UtcNow;
            
            if (Health <= 0)
            {
                // Lógica para quando o jogador morre
            }
        }
    }
}