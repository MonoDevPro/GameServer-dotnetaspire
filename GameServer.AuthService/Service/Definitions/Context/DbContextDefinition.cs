using GameServer.AuthService.Service.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace GameServer.AuthService.Service.Definitions.Context;

/// <summary>
/// ASP.NET Core services registration and configurations
/// </summary>
public static class DbContextDefinition
{
    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Configurar o contexto do banco de dados
        builder.Services.AddDbContext<AuthDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("authdb");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback para desenvolvimento local
                options.UseInMemoryDatabase("AuthServiceDevDb");
            }
            else
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(3);
                });
            }
        });
    }
    
    public static void ConfigureApplication(WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
        
        // Migrate the database
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        dbContext.Database.Migrate();
    }
}