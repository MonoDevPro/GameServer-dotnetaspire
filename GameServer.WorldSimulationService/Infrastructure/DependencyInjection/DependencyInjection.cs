using GameServer.WorldSimulationService.Application.Ports.In;
using GameServer.WorldSimulationService.Application.Ports.Out;
using GameServer.WorldSimulationService.Application.UseCases;
using GameServer.WorldSimulationService.Domain.Services;
using GameServer.WorldSimulationService.Infrastructure.Adapters.Out.Authentication;
using GameServer.WorldSimulationService.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.WorldSimulationService.Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds all the dependencies required for the World Simulation service using hexagonal architecture
        /// </summary>
        public static IServiceCollection AddHexagonalWorldSimulationService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register Domain Services
            services.AddScoped<WorldSimulationDomainService>();
            
            // Register Application Use Cases
            services.AddScoped<IWorldSimulationUseCase, Application.UseCases.WorldSimulationService>();
            
            // Register Infrastructure Adapters
            // Authentication
            services.AddScoped<IAuthenticationService, JwtAuthenticationService>();
            
            // Persistence
            services.AddScoped<IWorldRepository, WorldRepository>();
            
            return services;
        }
    }
}