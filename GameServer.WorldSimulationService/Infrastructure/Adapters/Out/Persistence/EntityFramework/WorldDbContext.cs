using Microsoft.EntityFrameworkCore;
using GameServer.WorldSimulationService.Domain.Entities;
using System.Numerics;
using System.Text.Json;
using System;
using System.Linq;
using GameServer.WorldService.Domain.Entities;
using GameServer.WorldSimulationService.Domain.Events;

namespace GameServer.WorldSimulationService.Infrastructure.Adapters.Out.Persistence.EntityFramework
{
    public class WorldDbContext : DbContext
    {
        public DbSet<World> Worlds { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<NPC> NPCs { get; set; }

        public WorldDbContext(DbContextOptions<WorldDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Ignore<DomainEvent>();
            
            // Configure Entity base class
            modelBuilder.Entity<Entity>(entity =>
            {
                entity.UseTpcMappingStrategy();
                entity.HasKey(e => e.Id);
            });

            // Configure World entity
            modelBuilder.Entity<World>(entity =>
            {
                entity.HasBaseType<Entity>();
                
                entity.ToTable("Worlds");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.LastUpdated).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                
                // One-to-many relationship with regions
                entity.HasMany(e => e.Regions)
                    .WithOne(r => r.World)
                    .HasForeignKey(r => r.WorldId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Region entity
            modelBuilder.Entity<Region>(entity =>
            {
                entity.HasBaseType<Entity>();
                
                entity.ToTable("Regions");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Width).IsRequired();
                entity.Property(e => e.Height).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.LastUpdated).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.WorldPosition).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Vector2>(v, (JsonSerializerOptions)null));
                
                // One-to-many relationship with entities (players and NPCs will be handled in their own tables)
                
                entity.HasMany(r => r.Entities)
                    .WithOne(e => e.Region)            // especifica a navegação inversa
                    .HasForeignKey(e => e.RegionId)    // usa a propriedade CLR correta
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            modelBuilder.Entity<GameEntity>(entity =>
            {
                entity.HasBaseType<Entity>();
                
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Position).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Vector3>(v, (JsonSerializerOptions)null));
                entity.Property(e => e.Rotation).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Quaternion>(v, (JsonSerializerOptions)null));
                entity.Property(e => e.Scale).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Vector3>(v, (JsonSerializerOptions)null));
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.LastUpdated).IsRequired();
            });

            // Configure Player entity derivada de GameEntity
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasBaseType<GameEntity>();
                
                entity.ToTable("Players");
                entity.Property(e => e.CharacterId).IsRequired();
                entity.Property(e => e.AccountId).IsRequired();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Health).IsRequired();
                entity.Property(e => e.MaxHealth).IsRequired();
                entity.Property(e => e.IsOnline).IsRequired();
                entity.Property(e => e.LastLogin).IsRequired();
            });

            // Configure NPC entity derivada de GameEntity
            modelBuilder.Entity<NPC>(entity =>
            {
                entity.HasBaseType<GameEntity>();
                
                entity.ToTable("NPCs");
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Health).IsRequired();
                entity.Property(e => e.MaxHealth).IsRequired();
                entity.Property(e => e.IsInteractable).IsRequired();
                entity.Property(e => e.Dialogue).HasMaxLength(1000);
                entity.Property(e => e.MovementCenter).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Vector3>(v, (JsonSerializerOptions)null));
            });
        }
    }
}