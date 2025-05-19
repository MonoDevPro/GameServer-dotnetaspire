using GameServer.AuthService.Service.Application.Data;

namespace GameServer.AuthService.Service.Definitions.Cors;

/// <summary>
/// Cors configurations
/// </summary>
public static class CorsDefinition
{
    /// <summary>
    /// Configure services for current application
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(AppData.CorsPolicyName, policyBuilder =>
            {
                var buildServiceProvider = builder.Services.BuildServiceProvider();
                policyBuilder.WithOrigins(buildServiceProvider
                    .GetRequiredService<IConfiguration>()
                    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }
    
    public static void ConfigureApplication(WebApplication app)
    {
        app.UseCors(AppData.CorsPolicyName);
    }
}
