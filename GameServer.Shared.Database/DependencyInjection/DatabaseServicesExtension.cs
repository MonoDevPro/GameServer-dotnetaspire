using GameServer.Shared.Database.Health;
using GameServer.Shared.Database.Repository.Reader;
using GameServer.Shared.Database.Repository.Writer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.Database.DependencyInjection;

public static class DatabaseServicesExtension
{
    /// <summary>
    /// Configura os serviços de banco de dados
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    public static void ConfigureDatabaseServices<TEntity, TContext>(this IServiceCollection services)
    where TEntity : class
    where TContext : DbContext
    {
        // Adiciona o repositório de escrita
        services.AddScoped<IWriterRepository<TEntity>>(opt =>
        {
            var dbContext = opt.GetRequiredService<TContext>();
            return new WriterRepository<TEntity>(dbContext, opt.GetRequiredService<ILogger<WriterRepository<TEntity>>>());
        });
        
        // Adiciona o repositório de leitura
        services.AddScoped<IReaderRepository<TEntity>>(opt =>
        {
            var dbContext = opt.GetRequiredService<TContext>();
            return new ReaderRepository<TEntity>(dbContext.Set<TEntity>());
        });
        
        // Adiciona a semente do banco de dados
        services.AddSingleton<IHostedService, DatabaseSeederService<TContext>>();
        
        // Configura o health check do banco de dados
        services.AddSingleton<IHealthCheck, DatabaseHealthCheck<TContext>>();
    }
}