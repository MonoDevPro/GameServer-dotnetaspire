namespace GameServer.AuthService.Service.Application.Data;

/// <summary>
/// Static data container
/// </summary>
public static class AppData
{
    /// <summary>
    /// Current service name
    /// </summary>
    public const string ServiceName = "Identity Service";
    
    public const string ServiceDescription = "Identity Service for Game Server";

    /// <summary>
    /// Default policy name for CORS
    /// </summary>
    public const string CorsPolicyName = "CorsPolicy";

    /// <summary>
    /// Default policy name for API
    /// </summary>
    public const string PolicyDefaultName = "DefaultPolicy";

    /// <summary>
    /// "SystemAdministrator"
    /// </summary>
    public const string SystemAdministratorRoleName = "Administrator";

    /// <summary>
    /// "BusinessOwner"
    /// </summary>
    public const string ManagerRoleName = "Manager";


    /// <summary>
    /// Roles
    /// </summary>
    public static IEnumerable<string> Roles
    {
        get
        {
            yield return SystemAdministratorRoleName;
            yield return ManagerRoleName;
        }
    }
}