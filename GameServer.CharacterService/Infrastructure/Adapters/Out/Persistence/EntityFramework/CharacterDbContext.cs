using GameServer.CharacterService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Persistence.EntityFramework;

public class CharacterDbContext : DbContext
{
    public DbSet<Character> Characters { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<AccountCache> AccountCaches { get; set; }

    public CharacterDbContext(DbContextOptions<CharacterDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configura a entidade Character
        modelBuilder.Entity<Character>(entity =>
        {
            entity.ToTable("Characters");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Class).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AccountId).IsRequired();
            entity.Property(e => e.Level).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastUpdatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();

            // Índice para busca por AccountId
            entity.HasIndex(e => e.AccountId);
            // Índice para busca por nome (deve ser único)
            entity.HasIndex(e => e.Name).IsUnique();
            
            // Relacionamento one-to-many com InventoryItem
            entity.HasMany(c => c.Inventory)
                .WithOne(i => i.Character)
                .HasForeignKey(i => i.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configura a entidade InventoryItem
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("InventoryItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CharacterId).IsRequired();
            entity.Property(e => e.ItemId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.IsEquipped).IsRequired();
            entity.Property(e => e.AcquiredAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired(false);

            // Índice para busca por CharacterId
            entity.HasIndex(e => e.CharacterId);
        });

        // Configura a entidade AccountCache
        modelBuilder.Entity<AccountCache>(entity =>
        {
            entity.ToTable("AccountCaches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.LastUpdatedAt).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}