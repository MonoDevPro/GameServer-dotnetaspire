using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.OpenIddict;
using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server;
using OpenIddict.Validation;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

public static class OpenIddictExtension
{
    public static WebApplicationBuilder ConfigureOpenIddict(this WebApplicationBuilder builder)
    {
        // 1) Carrega a seção OpenIddict do appsettings.json
        builder.Services.Configure<OpenIddictSettings>(
            builder.Configuration.GetSection("OpenIddict"));
        
        // Configurar OpenIddict completo
        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<AccountDbContext>();
            })
            .AddServer(options =>
            {
                /*options.AddEventHandler(OpenIddictServerHandlerDescriptor.CreateBuilder<T>() (builder =>
                    builder.UseInlineHandler(context =>
                    {
                        // adiciona um campo customizado:
                        context.Response["x-custom"] = "meu-valor";
                        return default;
                    })
                ));*/
                
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
        
        return builder;
    }
    
    public static async Task SeedOpenIddictClientsAsync(IServiceProvider provider)
    {
        // Traz a configuração
        var settings = provider
            .GetRequiredService<IOptions<OpenIddictSettings>>()
            .Value;

        // Manager para CRUD de clients
        var manager = provider
            .GetRequiredService<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>();

        foreach (var appSettings in settings.Applications)
        {
            if (await manager.FindByClientIdAsync(appSettings.ClientId) is null)
            {
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId         = appSettings.ClientId,
                    ClientSecret     = appSettings.ClientSecret,
                    DisplayName      = appSettings.DisplayName
                };

                // URIs
                foreach (var uri in appSettings.RedirectUris)
                    descriptor.RedirectUris.Add(new Uri(uri));
                foreach (var uri in appSettings.PostLogoutRedirectUris)
                    descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

                // Permissões
                foreach (var permission in appSettings.Permissions)
                    descriptor.Permissions.Add(permission);

                await manager.CreateAsync(descriptor);
            }
        }
    }
}