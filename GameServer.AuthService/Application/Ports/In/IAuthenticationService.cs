using GameServer.AuthService.Application.Models;

namespace GameServer.AuthService.Application.Ports.In;

/// <summary>
/// Porta de entrada para o serviço de autenticação
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registra um novo usuário no sistema
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    
    /// <summary>
    /// Autentica um usuário e retorna um token JWT se bem-sucedido
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);
    
    /// <summary>
    /// Valida um token JWT
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Obtém o motivo da expiração de um token JWT
    /// </summary>
    Task<string?> GetTokenExpirationReasonAsync(string token);
}