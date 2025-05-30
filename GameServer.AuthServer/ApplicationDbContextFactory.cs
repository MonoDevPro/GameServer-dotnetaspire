using GameServer.AuthServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameServer.AuthServer;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use PostgreSQL for design-time
        optionsBuilder.UseNpgsql("Host=localhost;Database=authdb;Username=postgres;Password=postgres");

        // Configure OpenIddict
        optionsBuilder.UseOpenIddict();

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
