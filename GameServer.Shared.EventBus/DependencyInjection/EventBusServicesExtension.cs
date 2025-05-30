using GameServer.Shared.EventBus.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.EventBus.DependencyInjection;

public class EventBusServicesExtension
{
    /// <summary>
    /// Configura os serviços de banco de dados
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    public static void ConfigureEventBus<TEvent>(IServiceCollection services)
    {
        // Adiciona o EventBus InMemory
        services.AddSingleton<IEventBus, InMemoryEventBus>();
    }
}