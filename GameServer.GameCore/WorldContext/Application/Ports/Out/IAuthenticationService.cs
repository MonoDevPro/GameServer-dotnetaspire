using System.Threading.Tasks;

namespace GameServer.WorldSimulationService.Application.Ports.Out
{
    /// <summary>
    /// Port for verifying authentication and authorization
    /// </summary>
    public interface IAuthenticationService
    {
        Task<bool> ValidateTokenAsync(string token);
        Task<bool> IsUserAuthorizedAsync(string userId, string requiredPermission);
    }
}