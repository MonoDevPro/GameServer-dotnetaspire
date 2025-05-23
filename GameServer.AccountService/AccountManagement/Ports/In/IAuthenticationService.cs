
namespace GameServer.AccountService.Service.Application.Ports.In;

/// <summary>
/// Interface para o serviço de autenticação na camada de aplicação
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Realiza login com nome de usuário/email e senha
    /// </summary>
    /// <param name="request">Requisição de login</param>
    /// <returns>Resposta de login com token e informações do usuário</returns>
    //Task<LoginResponse> LoginAsync(LoginRequest request);
    
    /// <summary>
    /// Atualiza o token de acesso usando um refresh token
    /// </summary>
    /// <param name="request">Requisição de atualização de token</param>
    /// <param name="ipAddress">Endereço IP do cliente</param>
    /// <param name="userAgent">User-Agent do cliente</param>
    /// <returns>Resposta com novos tokens</returns>
    //Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
    
    /// <summary>
    /// Realiza logout, invalidando o refresh token
    /// </summary>
    /// <param name="request">requisição de logout</param>
    /// <returns>Resultado da operação de logout</returns>
    //Task<LogoutResponseDto> LogoutAsync(LogoutRequest request);
    
    /// <summary>
    /// Verifica se o token de acesso é válido
    /// </summary>
    /// <param name="token">Token de acesso</param>
    /// <returns>Verdadeiro se o token for válido</returns>
    Task<bool> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Obtém o usuário atual a partir do token ou claims
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Dados do usuário ou nulo se não autenticado</returns>
    //Task<UserInfo?> GetCurrentUserAsync(Guid userId);
}