using GameServer.AccountService.Service.Application;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

/// <summary>
/// Cors configurations
/// </summary>
public static class CorsExtension
{
    /// <summary>
    /// Configure services for current application
    /// </summary>
    /// <param name="builder"></param>
    public static WebApplicationBuilder ConfigureCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(AccountServicesExtension.CorsPolicyName, policyBuilder =>
            {
                var buildServiceProvider = builder.Services.BuildServiceProvider();
                policyBuilder.WithOrigins(buildServiceProvider
                    .GetRequiredService<IConfiguration>()
                    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowed((host) =>
                    {
                        var allowedHosts = buildServiceProvider
                            .GetRequiredService<IConfiguration>()
                            .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                        return allowedHosts.Contains(host);
                    });
            });
        });

        return builder;
    }

    public static WebApplication UseCorsApplication(this WebApplication app)
    {
        app.UseCors(AccountServicesExtension.PolicyDefaultName);
        app.UseCors(AccountServicesExtension.CorsPolicyName);

        return app;
    }
}
