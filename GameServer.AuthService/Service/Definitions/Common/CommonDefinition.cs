namespace GameServer.AuthService.Service.Definitions.Common;

/// <summary>
/// AspNetCore common configuration
/// </summary>
public static class CommonDefinition
{
    /// <summary>
    /// Configure application for current microservice
    /// </summary>
    /// <param name="app"></param>
    public static void ConfigureApplication(WebApplication app)
    {
        app.UseHttpsRedirection();
    }

    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddLocalization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddResponseCaching();
        builder.Services.AddMemoryCache();
    }
}