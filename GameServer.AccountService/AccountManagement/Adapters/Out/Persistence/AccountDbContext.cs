using GameServer.AccountService.AccountManagement.Domain.Entities;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;

public class AccountDbContext : IdentityDbContextBase
{
    public DbSet<Account> Accounts { get; set; } = null!;

    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Mapeia as tabelas do OpenIddict
        modelBuilder.UseOpenIddict();
            
        // Configuração da entidade Account
        var accountEntity = modelBuilder.Entity<Account>();
        accountEntity.HasKey(a => a.Id);
        accountEntity.Property(a => a.IsActive);
        accountEntity.Property(a => a.AccountType);
        accountEntity.Property(a => a.CreatedAt);
        accountEntity.Property(a => a.LastLoginDate);
            
        // Configuração dos Value Objects
        accountEntity.OwnsOne(a => a.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").IsRequired();
        });
            
        accountEntity.OwnsOne(a => a.Username, username =>
        {
            username.Property(u => u.Value).HasColumnName("Username").IsRequired();
        });
            
        accountEntity.OwnsOne(a => a.Password, password =>
        {
            password.Property(p => p.Hash).HasColumnName("PasswordHash").IsRequired();
            password.Property(p => p.Hash).HasColumnName("PasswordStrength").IsRequired();
        });
            
        accountEntity.OwnsOne(a => a.BanInfo, banInfo =>
        {
            banInfo.Property(b => b.Status).HasColumnName("BanStatus");
            banInfo.Property(b => b.ExpiresAt).HasColumnName("BanExpiresAt");
            banInfo.Property(b => b.Reason).HasColumnName("BanReason");
            banInfo.Property(b => b.BannedById).HasColumnName("BannedById");
        });
            
        // Configuração do relacionamento com as roles
        modelBuilder.Entity<RoleVO>().HasKey(r => r.Value);
            
        accountEntity
            .HasMany(a => a.Roles)
            .WithMany()
            .UsingEntity(j => j.ToTable("AccountRoles"));
    }
}