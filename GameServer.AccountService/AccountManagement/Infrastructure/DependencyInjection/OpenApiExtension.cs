using GameServer.AccountService.AccountManagement.Adapters.Out.OpenApi;
using GameServer.AccountService.Service.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
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

        //builder.Services.AddSwaggerGen();
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<OAuth2SecuritySchemeTransformer>();
        });

        builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PCM", Version = "v1" });
    options.AddSecurityDefinition("Authentication", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OpenIdConnect,
        Description = "Description",
        In = ParameterLocation.Header,
        Name = HeaderNames.Authorization,
        Flows = new OpenApiOAuthFlows
        {
            ClientCredentials = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("/connect/token", UriKind.Relative),
                TokenUrl = new Uri("/connect/token", UriKind.Relative)
            }
        },
        OpenIdConnectUrl = new Uri("/.well-known/openid-configuration", UriKind.Relative)
    });

    options.AddSecurityRequirement(
                        new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                        { Type = ReferenceType.SecurityScheme, Id = "oauth" },
                                },
                                Array.Empty<string>()
                            }
                        }
                    );

});



        return builder;
    }

    public static WebApplication UseApplicationOpenApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;

        app.MapOpenApi();
        // app.UseApplicationOpenApi(); // Remover chamada recursiva!
        // Se quiser Swagger tradicional, descomente as linhas abaixo e garanta AddSwaggerGen() no builder
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(OpenApiConfig, $"{AccountServicesExtension.ServiceName} v.{AppVersion}");
            options.DocumentTitle = $"{AccountServicesExtension.ServiceName}";
            options.DefaultModelExpandDepth(0);
            options.DefaultModelRendering(ModelRendering.Model);
            options.DefaultModelsExpandDepth(0);
            options.DocExpansion(DocExpansion.None);
            options.OAuthScopeSeparator(" ");
            options.OAuthClientId("web-client");
            options.OAuthClientSecret("web-client-secret");
            options.DisplayRequestDuration();
            options.OAuthAppName(AccountServicesExtension.ServiceName);
            options.OAuthUsePkce();
        });

        return app;
    }
}