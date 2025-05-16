using Microsoft.Extensions.DependencyInjection;
using GameServer.WorldService.Application.Ports;
using GameServer.WorldService.Application.Services;
using GameServer.WorldService.Infrastructure.Adapters;

namespace GameServer.WorldService.Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWorldServices(this IServiceCollection services)
        {
            // Registrar serviços da camada de aplicação
            services.AddScoped<IWorldService, Application.Services.WorldService>();
            
            // Registrar repositórios (implementação da infraestrutura)
            services.AddScoped<IWorldRepository, InMemoryWorldRepository>();
            
            return services;
        }
    }
}