using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Identity.OpenIddict;

public static class SeedManager
{
    public static async Task SeedAsync(IServiceProvider provider)
    {
        var manager = provider.GetRequiredService<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>>();

        if (await manager.FindByClientIdAsync("my_client") == null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "my_client",
                ClientSecret = "secret",
                DisplayName = "My Client App",
                Permissions =
                {
                    //Permissions.Endpoints.Token,
                    //Permissions.GrantTypes.Password,
                    //Permissions.Scopes.Email,
                    //Permissions.Scopes.Profile
                }
            });
        }
    }

}