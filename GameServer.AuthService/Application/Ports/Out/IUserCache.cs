using GameServer.AuthService.Domain.Entities;

namespace GameServer.AuthService.Application.Ports.Out;

/// <summary>
/// Interface para o serviço de cache de usuários
/// </summary>
public interface IUserCache
{
    /// <summary>
    /// Tenta obter um usuário do cache pelo ID
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>O usuário se estiver no cache, ou nulo</returns>
    Task<User?> GetUserByIdAsync(Guid id);
    
    /// <summary>
    /// Tenta obter um usuário do cache pelo nome de usuário
    /// </summary>
    /// <param name="username">Nome do usuário</param>
    /// <returns>O usuário se estiver no cache, ou nulo</returns>
    Task<User?> GetUserByUsernameAsync(string username);
    
    /// <summary>
    /// Tenta obter um usuário do cache pelo email
    /// </summary>
    /// <param name="email">Email do usuário</param>
    /// <returns>O usuário se estiver no cache, ou nulo</returns>
    Task<User?> GetUserByEmailAsync(string email);
    
    /// <summary>
    /// Armazena ou atualiza um usuário no cache
    /// </summary>
    /// <param name="user">Usuário a ser armazenado</param>
    Task SetUserAsync(User user);
    
    /// <summary>
    /// Remove um usuário do cache
    /// </summary>
    /// <param name="id">ID do usuário a ser removido</param>
    Task RemoveUserAsync(Guid id);
    
    /// <summary>
    /// Verifica se um token é válido no cache
    /// </summary>
    /// <param name="token">Token a ser validado</param>
    /// <returns>True se o token for válido, false caso contrário</returns>
    Task<bool> IsTokenValidAsync(string token);
    
    /// <summary>
    /// Armazena um token válido no cache
    /// </summary>
    /// <param name="token">Token a ser armazenado</param>
    /// <param name="userId">ID do usuário associado ao token</param>
    /// <param name="expiration">Data de expiração do token</param>
    Task AddValidTokenAsync(string token, Guid userId, DateTime expiration);
    
    /// <summary>
    /// Invalida um token no cache
    /// </summary>
    /// <param name="token">Token a ser invalidado</param>
    Task InvalidateTokenAsync(string token);
}