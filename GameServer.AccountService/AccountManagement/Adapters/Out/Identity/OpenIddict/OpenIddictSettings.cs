namespace GameServer.AccountService.AccountManagement.Adapters.Out.Identity.OpenIddict;

public class OpenIddictSettings
{
    public List<OpenIddictApplicationSettings> Applications { get; set; }
}

public class OpenIddictApplicationSettings
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string DisplayName { get; set; }
    public List<string> RedirectUris { get; set; }
    public List<string> PostLogoutRedirectUris { get; set; }
    public List<string> Permissions { get; set; }
}