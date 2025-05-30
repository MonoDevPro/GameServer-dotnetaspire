namespace GameServer.WorldSimulationService.Infrastructure.Adapters.Out.Authentication;

/// <summary>
/// Configurações do JWT
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public List<string> Audiences { get; set; } = [];
    public int ExpiresInMinutes { get; set; } = 60;
}