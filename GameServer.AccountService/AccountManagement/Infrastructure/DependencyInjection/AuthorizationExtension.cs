using System.Text.Json;
using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.OpenIddict;
using GameServer.AccountService.Service.Application;
using GameServer.AccountService.Service.Definitions.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server.AspNetCore;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

/// <summary>
/// Authorization Policy registration
/// </summary>
public static class AuthorizationExtension
{
    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public static WebApplicationBuilder ConfigureAuthorization(this WebApplicationBuilder builder)
    {
        // Configurar autorização
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AccountServicesExtension.PolicyDefaultName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "api");
            });
        });

        builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
        builder.Services.AddSingleton<IAuthorizationHandler, AppPermissionHandler>();
        
        return builder;
    }

    /// <summary>
    /// Configure application for current microservice
    /// </summary>
    /// <param name="app"></param>
    public static WebApplication UseApplicationAuthorization(this WebApplication app)
    {
        app.UseAuthorization();
        
        return app;
    }
}
