using GameServer.CharacterService.Application.Ports.In;
using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Infrastructure.Adapters.Out.Cache;
using GameServer.CharacterService.Infrastructure.Adapters.Out.Persistence;
using GameServer.CharacterService.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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
        services.AddSingleton<Domain.Services.CharacterCreationService>();
        services.AddSingleton<Domain.Services.InventoryService>();
        
        // 2. Registrar portas de entrada (implementações concretas)
        services.AddScoped<ICharacterUseCases, Application.Services.CharacterService>();
        services.AddScoped<IInventoryUseCases, Application.Services.InventoryService>();
        
        // 3. Registrar portas de saída (interfaces)
        
        // 3.2 Registrar implementações dos repositórios
        services.AddScoped<ICharacterRepository, EfCharacterRepository>();
        services.AddScoped<IInventoryRepository, EfInventoryRepository>();
        
        return services;
    }

    public static IServiceCollection AddCharacterServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuração do cache
        services.Configure<CharacterCacheOptions>(
            configuration.GetSection(CharacterCacheOptions.SectionName));

        // Registrar MemoryCache se ainda não estiver registrado
        services.AddMemoryCache(options =>
        {
            // Busca o limite de tamanho da configuração
            var sizeLimit = configuration.GetSection(CharacterCacheOptions.SectionName)
                .GetValue<int?>("SizeLimit") ?? 1000;
            
            options.SizeLimit = sizeLimit;
        });
        
        // Registrar serviço de cache
        services.AddSingleton<ICharacterCache, MemoryCharacterCache>();
        
        // Registrar repositórios
        // Note que o registro concreto de ICharacterRepository e IInventoryRepository
        // precisa ser feito antes de chamada este método
        
        // Registrar wrappers com cache - decorador dos repositórios concretos
        services.Decorate<ICharacterRepository, CachedCharacterRepository>();
        services.Decorate<IInventoryRepository, CachedInventoryRepository>();
        
        return services;
    }
}