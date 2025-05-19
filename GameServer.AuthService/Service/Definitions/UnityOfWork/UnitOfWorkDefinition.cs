using GameServer.AuthService.Service.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using GameServer.AuthService.Service.Infrastructure.UnityOfWork;

namespace GameServer.AuthService.Service.Definitions.UnityOfWork;

/// <summary>
/// Unit of Work registration as MicroserviceDefinition
/// </summary>
public class UnitOfWorkDefinition
{
    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddUnitOfWork<AuthDbContext>();
    }
}