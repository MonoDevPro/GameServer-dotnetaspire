using GameServer.AuthService.Domain.Entities;

namespace GameServer.AuthService.Application.Ports.Out;

/// <summary>
/// Porta de saída para geração e validação de tokens
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Gera um token JWT para um usuário
    /// </summary>
    string GenerateToken(User user);
    
    /// <summary>
    /// Valida um token JWT
    /// </summary>
    bool ValidateToken(string token);
    
    /// <summary>
    /// Obtém o ID do usuário de um token JWT
    /// </summary>
    Guid? GetUserIdFromToken(string token);
    
    /// <summary>
    /// Obtém o motivo da expiração de um token JWT
    /// </summary>
    string? GetExpirationReason(string token);
}