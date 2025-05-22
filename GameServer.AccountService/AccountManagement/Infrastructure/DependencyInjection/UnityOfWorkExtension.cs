using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;
using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence.UnityOfWork;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

public class UnityOfWorkExtension
{
    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddUnitOfWork<AccountDbContext>();
    }
}