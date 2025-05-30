using GameServer.GameCore.AccountContext.Domain.Entities;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GameServer.GameCore.AccountContext.Adapters.Out.Persistence;

public class AccountDbContext : DbContext
{
    public DbSet<Account> Accounts { get; set; } = null!;

    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração da entidade Account
        var accountEntity = modelBuilder.Entity<Account>();
        accountEntity.HasKey(a => a.Id);
        accountEntity.Property(a => a.IsActive);
        accountEntity.Property(a => a.AccountType);
        accountEntity.Property(a => a.CreatedAt);
        accountEntity.Ignore(a => a.DomainEvents); // Ignora eventos de domínio para evitar loops infinitos

        // Configuração dos Value Objects com conversores customizados
        accountEntity.Property(a => a.Email)
            .HasConversion(
                v => v.Value, // Para o banco
                v => EmailVO.Create(v) // Para o domínio
            )
            .HasColumnName("Email").IsRequired();

        accountEntity.Property(a => a.Username)
            .HasConversion(
                v => v.Value,
                v => UsernameVO.Create(v)
            )
            .HasColumnName("Username").IsRequired();

        // PasswordVO como propriedade complexa (OwnsOne)
        accountEntity.OwnsOne(a => a.Password, password =>
        {
            password.Property(p => p.Hash)
                .HasColumnName("PasswordHash")
                .IsRequired();
            password.Property(p => p.Strength)
                .HasColumnName("PasswordStrength")
                .IsRequired();
        });

        // BanInfo pode ser mapeado como OwnsOne se for um VO complexo
        accountEntity.OwnsOne(a => a.BanInfo, banInfo =>
        {
            banInfo.Property(b => b.Status).HasColumnName("BanStatus");
            banInfo.Property(b => b.ExpiresAt).HasColumnName("BanExpiresAt");
            banInfo.Property(b => b.Reason).HasColumnName("BanReason");
            banInfo.Property(b => b.BannedById).HasColumnName("BannedById");
        });

        // LoginInfoVO como propriedade complexa (OwnsOne)
        accountEntity.OwnsOne(a => a.LastLoginInfo, loginInfo =>
        {
            loginInfo.Property(l => l.LastLoginIp)
                .HasColumnName("LastLoginIp")
                .IsRequired();
            loginInfo.Property(l => l.LastLoginDate)
                .HasColumnName("LastLoginDate")
                .IsRequired();
        });

        // Configuração do relacionamento com as roles
        modelBuilder.Entity<RoleVO>().HasKey(r => r.Value);
        accountEntity
            .HasMany(a => a.Roles)
            .WithMany()
            .UsingEntity(j => j.ToTable("AccountRoles"));
    }
}