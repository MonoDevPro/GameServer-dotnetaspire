using GameServer.AuthService.Application.Models;

namespace GameServer.AuthService.Application.Ports.In;

/// <summary>
/// Porta de entrada para gerenciamento de contas de usuário
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Obtém os detalhes da conta de um usuário
    /// </summary>
    Task<AccountResponse?> GetAccountDetailsAsync(Guid userId);
    
    /// <summary>
    /// Atualiza informações da conta do usuário
    /// </summary>
    Task<AccountResponse?> UpdateAccountAsync(Guid userId, UpdateAccountRequest request);
    
    /// <summary>
    /// Bane uma conta de usuário
    /// </summary>
    Task<bool> BanAccountAsync(Guid userId, DateTime until, string reason);
    
    /// <summary>
    /// Remove o banimento de uma conta de usuário
    /// </summary>
    Task<bool> UnbanAccountAsync(Guid userId);
    
    /// <summary>
    /// Desativa uma conta de usuário
    /// </summary>
    Task<bool> DeactivateAccountAsync(Guid userId);
    
    /// <summary>
    /// Ativa uma conta de usuário
    /// </summary>
    Task<bool> ActivateAccountAsync(Guid userId);
    
    /// <summary>
    /// Promove um usuário para administrador
    /// </summary>
    Task<bool> PromoteToAdminAsync(Guid userId);
    
    /// <summary>
    /// Remove os privilégios de administrador de um usuário
    /// </summary>
    Task<bool> DemoteFromAdminAsync(Guid userId);
    
    /// <summary>
    /// Verifica se um usuário é administrador
    /// </summary>
    Task<bool> IsAdminAsync(Guid userId);
}

