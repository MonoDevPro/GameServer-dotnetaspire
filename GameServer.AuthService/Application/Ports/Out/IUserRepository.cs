using GameServer.AuthService.Domain.Entities;

namespace GameServer.AuthService.Application.Ports.Out;

/// <summary>
/// Porta de saída para operações de usuários no repositório
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Obtém um usuário pelo ID
    /// </summary>
    Task<User?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Obtém um usuário pelo nome de usuário
    /// </summary>
    Task<User?> GetByUsernameAsync(string username);
    
    /// <summary>
    /// Obtém um usuário pelo e-mail
    /// </summary>
    Task<User?> GetByEmailAsync(string email);
    
    /// <summary>
    /// Cria um novo usuário
    /// </summary>
    Task<User> CreateAsync(User user);
    
    /// <summary>
    /// Atualiza um usuário existente
    /// </summary>
    Task<bool> UpdateAsync(User user);
    
    /// <summary>
    /// Verifica se um nome de usuário já existe
    /// </summary>
    Task<bool> UsernameExistsAsync(string username);
    
    /// <summary>
    /// Verifica se um e-mail já existe
    /// </summary>
    Task<bool> EmailExistsAsync(string email);
}