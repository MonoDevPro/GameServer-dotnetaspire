using GameServer.AuthService.Service.Application.Factories;
using GameServer.AuthService.Service.Definitions.Authorization;
using GameServer.AuthService.Service.Definitions.Common;
using GameServer.AuthService.Service.Definitions.Context;
using GameServer.AuthService.Service.Definitions.Cors;
using GameServer.AuthService.Service.Definitions.ErrorHandler;
using GameServer.AuthService.Service.Definitions.FluentValidating;
using GameServer.AuthService.Service.Definitions.OpenApi;
using GameServer.AuthService.Service.Definitions.OpenIddict;
using GameServer.AuthService.Service.Definitions.RateLimiter;
using GameServer.AuthService.Service.Definitions.UnityOfWork;

namespace GameServer.AuthService.Service.Definitions.DependencyInjection;

public static class AuthServicesExtension
{
    public static WebApplicationBuilder ConfigureAuthServices(this WebApplicationBuilder builder)
    {
        // Adiciona os serviços 
        CommonDefinition.ConfigureServices(builder);
        CorsDefinition.ConfigureServices(builder);
        DbContextDefinition.ConfigureServices(builder);
        OpenApiDefinition.ConfigureServices(builder);
        UnitOfWorkDefinition.ConfigureServices(builder);
        FluentValidationDefinition.ConfigureServices(builder);
        AuthorizationDefinition.ConfigureServices(builder);
        OpenIddictDefinition.ConfigureServices(builder);
        RateLimiterDefinition.ConfigureServices(builder);
        builder.Services.AddScoped<ApplicationUserClaimsPrincipalFactory>();
        
        return builder;
    }
    
    public static WebApplication ConfigureAuthApplication(this WebApplication app)
    {
        // Adiciona os middlewares
        CommonDefinition.ConfigureApplication(app);
        OpenApiDefinition.ConfigureApplication(app);
        AuthorizationDefinition.ConfigureApplication(app);
        ErrorHandlingDefinition.ConfigureApplication(app); 
        RateLimiterDefinition.ConfigureApplication(app);
        DbContextDefinition.ConfigureApplication(app);
        CorsDefinition.ConfigureApplication(app);
        
        return app;
    }
}