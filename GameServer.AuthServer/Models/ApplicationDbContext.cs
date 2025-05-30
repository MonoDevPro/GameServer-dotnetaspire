using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GameServer.AuthServer.Models;

/// <summary>
/// Base DbContext with predefined configuration
/// </summary>
public class ApplicationDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
        
    }

    /// <summary>
    /// Configures the schema needed for the identity framework.
    /// </summary>
    /// <param name="builder">
    /// The builder being used to construct the model for this context.
    /// </param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configurações específicas do OpenIddict
        builder.UseOpenIddict();

        // Renomear tabelas do Identity para manter consistência de nomenclatura
        builder.Entity<IdentityUser<Guid>>().ToTable("IdentityUsers");
        builder.Entity<IdentityRole<Guid>>().ToTable("IdentityRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("IdentityUserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("IdentityUserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("IdentityRoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("IdentityUserTokens");
    }
}