using GameServer.CharacterService.Application.Ports.In;
using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Application.Services;
using GameServer.CharacterService.Domain.Services;
using GameServer.CharacterService.Infrastructure.Adapters.Out.Cache;
using GameServer.CharacterService.Infrastructure.Adapters.Out.Messaging;
using GameServer.CharacterService.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;

namespace GameServer.CharacterService.Infrastructure.DependencyInjection;

/// <summary>
/// Classe para registrar os serviços da camada de aplicação e infraestrutura
/// </summary>
public static class CharacterServiceRegistration
{
    /// <summary>
    /// Registra os serviços da arquitetura hexagonal relacionados à este microserviço.
    /// </summary>
    public static IServiceCollection AddHexagonalCharacterService(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Registrar serviços de domínio
        services.AddSingleton<CharacterCreationService>();
        services.AddSingleton<GameServer.CharacterService.Domain.Services.InventoryService>();
        
        // 2. Registrar portas de entrada (implementações concretas)
        services.AddScoped<ICharacterUseCases, CharacterService.Application.Services.CharacterService>();
        services.AddScoped<IInventoryUseCases, Application.Services.InventoryService>();
        
        // 3. Registrar portas de saída (interfaces)
        
        // 3.1 Configurar o contexto do banco de dados
        services.AddDbContext<CharacterDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(CharacterDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3);
            });
        });
        
        // 3.2 Registrar implementações dos repositórios
        services.AddScoped<EfCharacterRepository>();
        services.AddScoped<EfInventoryRepository>();
        services.AddSingleton<MemoryAccountsCache>();
        
        // 3.3 Registrar wrappers de cache
        services.AddScoped<ICharacterRepository>(provider => 
            provider.GetRequiredService<EfCharacterRepository>());
            
        services.AddScoped<IInventoryRepository>(provider => 
            new RedisInventoryRepository(
                provider.GetRequiredService<EfInventoryRepository>(),
                provider.GetRequiredService<IDistributedCache>(),
                provider.GetRequiredService<ILogger<RedisInventoryRepository>>()));
                
        services.AddSingleton<IAccountsCache>(provider =>
            provider.GetRequiredService<MemoryAccountsCache>());
        
        // 3.4 Configurar RabbitMQ
        services.AddSingleton<IConnectionFactory>(provider =>
        {
            var host = configuration["RabbitMQ:Host"] ?? "localhost";
            var username = configuration["RabbitMQ:Username"] ?? "guest";
            var password = configuration["RabbitMQ:Password"] ?? "guest";
            var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
            
            return new ConnectionFactory
            {
                HostName = host,
                UserName = username,
                Password = password,
                Port = port,
                //DispatchConsumersAsync = true
            };
        });
        
        // 3.5 Registrar consumidor de eventos
        services.AddSingleton<IAccountEventsConsumer, RabbitMQAccountEventsConsumer>();
        services.AddHostedService<AccountEventsConsumerBackgroundService>();
        
        return services;
    }
}