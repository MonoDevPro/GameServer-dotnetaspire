using System.Text.Json;
using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.OpenIddict;
using GameServer.AccountService.Service.Definitions.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace GameServer.AccountService.Authentication.Infrastructure.Authorization;

/// <summary>
/// Authorization Policy registration
/// </summary>
public static class AuthenticationExtension
{
    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public static WebApplicationBuilder ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        var url = builder.Configuration.GetSection("AccountService").GetValue<string>("Url");

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });

        return builder;
    }

    /// <summary>
    /// Configure application for current microservice
    /// </summary>
    /// <param name="app"></param>
    public static WebApplication UseApplicationAuthentication(this WebApplication app)
    {
        app.UseAuthentication();

        return app;
    }
}
