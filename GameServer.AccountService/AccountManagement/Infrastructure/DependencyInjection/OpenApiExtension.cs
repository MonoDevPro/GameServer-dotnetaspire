using GameServer.AccountService.AccountManagement.Adapters.Out.OpenApi;
using GameServer.AccountService.Service.Application;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

/// <summary>
/// Swagger definition for application
/// </summary>
public static class OpenApiExtension
{
    // -------------------------------------------------------
    // ATTENTION!
    // -------------------------------------------------------
    // If you use are git repository then you can uncomment line with "ThisAssembly" below for versioning by GIT possibilities.
    // Otherwise, you can change versions of your API by manually.
    // If you are not going to use git-versioning, do not forget install package "GitInfo" 
    // private const string AppVersion = $"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch}";
    // -------------------------------------------------------
    public const string AppVersion = "9.0.6";
    private const string OpenApiConfig = "/openapi/v1.json";
    
    public static WebApplicationBuilder ConfigureOpenApi(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<OAuth2SecuritySchemeTransformer>();
        });

        return builder;
    }

    public static WebApplication UseApplicationOpenApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "GameServer Auth API V1");
        
            options.DocumentTitle = $"{AccountServicesExtension.ServiceName}";
            options.DefaultModelExpandDepth(0);
            options.DefaultModelRendering(ModelRendering.Model);
            options.DefaultModelsExpandDepth(0);
            options.DocExpansion(DocExpansion.None);
            options.OAuthScopeSeparator(" ");
            options.OAuthClientId("client-id-code");
            options.OAuthClientSecret("client-secret-code");
            options.DisplayRequestDuration();
            options.OAuthAppName(AccountServicesExtension.ServiceName);
            options.OAuthUsePkce();
        });
        
        return app;
    }
}