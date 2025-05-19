using GameServer.AuthService.Service.Infrastructure.Adapters.Out.Persistence.EntityFramework;

namespace GameServer.AuthService.Service.Definitions.OpenIddict;

public static class OpenIddictDefinition
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Configurar OpenIddict completo
        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<AuthDbContext>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("connect/token")
                    .SetAuthorizationEndpointUris("connect/authorize")
                    .SetUserInfoEndpointUris("connect/userinfo")
                    .SetIntrospectionEndpointUris("connect/introspect");
                
                // Flows permitidos
                options.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow()
                    .AllowPasswordFlow();
                
                // Certificados para desenvolvimento
                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();
                
                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });
    }
}