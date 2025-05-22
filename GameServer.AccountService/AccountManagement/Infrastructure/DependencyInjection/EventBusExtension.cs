using GameServer.AccountService.AccountManagement.Adapters.Out.Messaging;
using GameServer.AccountService.AccountManagement.Ports.Out.Messaging;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

public static class EventBusExtension
{
    public static WebApplicationBuilder ConfigureInMemoryEventBus(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
        return builder;
    }
}