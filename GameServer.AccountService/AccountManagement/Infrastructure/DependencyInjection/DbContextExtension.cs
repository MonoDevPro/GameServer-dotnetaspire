using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

/// <summary>
/// ASP.NET Core services registration and configurations
/// </summary>
public static class DbContextExtension
{
    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public static WebApplicationBuilder ConfigureDbContext(
        this WebApplicationBuilder builder, 
        string? connectionString = null)
    {
        // Configurar o contexto do banco de dados
        builder.Services.AddDbContext<AccountDbContext>(options =>
        {
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
            
            options.UseOpenIddict(); // essencial para mapear as entidades
        });
        
        return builder;
    }
    
    public static WebApplication UseApplicationDbContext(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        
        // Migrate the database
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        dbContext.Database.Migrate();
        
        return app;
    }
}