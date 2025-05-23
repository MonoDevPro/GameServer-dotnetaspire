using System;
using GameServer.AccountService.AccountManagement.Application.CQRS.Container;
using GameServer.AccountService.Authentication.Infrastructure.Authorization;
using GameServer.AccountService.Service.Application;
using GameServer.ServiceDefaults;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

public static class AccountServicesExtension
{
    /// <summary>
    /// Current service name
    /// </summary>
    public const string ServiceName = "Account Service";
    public const string ServiceDescription = "Account Service for Game Server";

    /// <summary>
    /// "SystemAdministrator"
    /// </summary>
    public const string SystemAdministratorRoleName = "Administrator";

    /// <summary>
    /// "BusinessOwner"
    /// </summary>
    public const string ManagerRoleName = "Manager";

    /// <summary>
    /// Default policy name for CORS
    /// </summary>
    public const string CorsPolicyName = "CorsPolicy";

    /// <summary>
    /// Default policy name for API
    /// </summary>
    public const string PolicyDefaultName = "DefaultPolicy";

    /// <summary>
    /// Roles
    /// </summary>
    public static IEnumerable<string> Roles
    {
        get
        {
            yield return SystemAdministratorRoleName;
            yield return ManagerRoleName;
        }
    }

    public static WebApplicationBuilder ConfigureAccountServices(this WebApplicationBuilder builder)
    {
        // Add service defaults & Aspire client integrations
        builder.AddServiceDefaults();

        builder.Services.AddLocalization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddResponseCaching();
        builder.Services.AddMemoryCache();

        // Add services to the container.
        builder.Services.AddProblemDetails();
        builder.Services.AddControllers();

        var connectionString = builder.Configuration.GetConnectionString("accountdb");

        // Adiciona os serviços 
        builder
            .ConfigureCQRS()
            .ConfigureInMemoryEventBus()
            .ConfigureCors()
            .ConfigureAuthentication()
            .ConfigureAuthorization()
            .ConfigureIdentity()
            .ConfigureOpenIddict()
            .ConfigureOpenApi()
            .ConfigureDbContext(connectionString)
            .ConfigureRepositories()
            .ConfigureRateLimiter()
            .ConfigureFluentValidation()
            ;

        return builder;
    }

    public static WebApplication UseAccountApplication(this WebApplication app)
    {
        // Adiciona os middlewares
        app.UseRouting();
        app.UseHttpsRedirection();

        app
            .UseApplicationAuthentication()
            .UseApplicationAuthorization()
            .UseCorsApplication()
            .UseApplicationIdentity()
            .UseApplicationOpenApi()
            .UseApplicationDbContext()
            .UseApplicationErrorHandler()
            ;

        // Map default health check endpoints
        app.MapDefaultEndpoints();
        app.MapControllers();


        return app;
    }
}
