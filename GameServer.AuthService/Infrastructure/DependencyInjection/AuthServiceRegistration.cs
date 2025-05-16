using GameServer.AuthService.Application.Ports.In;
using GameServer.AuthService.Application.Ports.Out;
using GameServer.AuthService.Application.Services;
using GameServer.AuthService.Infrastructure.Adapters.Out.Authentication;
using GameServer.AuthService.Infrastructure.Adapters.Out.Cache;
using GameServer.AuthService.Infrastructure.Adapters.Out.Persistence;
using GameServer.AuthService.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using GameServer.AuthService.Infrastructure.Adapters.Out.Persistence.InMemory;
using GameServer.AuthService.Infrastructure.Adapters.Out.Security;
using Microsoft.EntityFrameworkCore;

namespace GameServer.AuthService.Infrastructure.DependencyInjection;

/// <summary>
/// Classe para registrar os serviços da camada de aplicação
/// </summary>
public static class AuthServiceRegistration
{
    /// <summary>
    /// Registra os serviços da arquitetura hexagonal relacionados à este microserviço.
    /// </summary>
    public static IServiceCollection AddHexagonalAuthService(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Implementações concretas das portas de entrada
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAccountService, AccountService>();
        // 2. Implementações concretas das portas de saída
        services.AddSingleton<IUserCache, MemoryUserCache>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // 3. Implementações concretas dos adaptadores de saída
        services.AddSingleton<JwtTokenGenerator>();

        // Repositorios
        // -> EfCore
        services.AddScoped<EfUserRepository>();
        // -> InMemory - para ativar, descomentar a linha abaixo e comentar a do EntityFramework
        //services.AddSingleton<InMemoryUserRepository>();

        // 3.1. Wrappers com cache para os adaptadores de saída
        services.AddScoped<IUserRepository>(provider => 
            new CachedUserRepository(
                provider.GetRequiredService<EfUserRepository>(),
                provider.GetRequiredService<IUserCache>(),
                provider.GetRequiredService<ILogger<CachedUserRepository>>()));

        services.AddSingleton<ITokenGenerator>(provider => 
            new CachedTokenGenerator(
                provider.GetRequiredService<JwtTokenGenerator>(),
                provider.GetRequiredService<IUserCache>(),
                provider.GetRequiredService<ILogger<CachedTokenGenerator>>()));
        
        return services;
    }
}