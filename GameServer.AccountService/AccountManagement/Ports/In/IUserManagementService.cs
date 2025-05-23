
namespace GameServer.AccountService.Service.Application.Ports.In;

/// <summary>
/// Interface para o serviço de gerenciamento de usuários
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Obtém um usuário pelo ID
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Dados do usuário ou null se não encontrado</returns>
    //Task<UserInfo?> GetUserByIdAsync(Guid userId);
    
    /// <summary>
    /// Obtém um usuário pelo nome de usuário
    /// </summary>
    /// <param name="request">Request de consulta</param>
    /// <returns>Dados dos usuários</returns>
    //Task<UserFilterResponse> GetUserAsync(UserFilterRequest request);
    
    /// <summary>
    /// Obtém um usuário pelo email
    /// </summary>
    /// <param name="email">Email do usuário</param>
    /// <returns>Dados do usuário ou null se não encontrado</returns>
    //Task<UserInfo?> GetUserByEmailAsync(string email);
    
    /// <summary>
    /// Atualiza o perfil de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="request">Dados de atualização</param>
    /// <returns>Dados atualizados do usuário ou null em caso de erro</returns>
    //Task<UserInfo?> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request);
    
    /// <summary>
    /// Altera o tipo de conta de um usuário
    /// </summary>
    /// <param name="request">Requisição com os dados de alteração</param>
    /// <param name="adminUserId">ID do administrador que está realizando a alteração</param>
    /// <returns>Dados atualizados do usuário ou null em caso de erro</returns>
    //Task<UserInfo?> ChangeAccountTypeAsync(ChangeAccountTypeRequest request, Guid adminUserId);
    
    /// <summary>
    /// Bane um usuário
    /// </summary>
    /// <param name="request">Requisição com os dados de banimento</param>
    /// <param name="adminUserId">ID do administrador que está aplicando o banimento</param>
    /// <returns>Dados atualizados do usuário ou null em caso de erro</returns>
    //Task<UserInfo?> BanUserAsync(BanUserRequest request, Guid adminUserId);
    
    /// <summary>
    /// Remove o banimento de um usuário
    /// </summary>
    /// <param name="request">Requisição com os dados de desbanimento</param>
    /// <param name="adminUserId">ID do administrador que está removendo o banimento</param>
    /// <returns>Dados atualizados do usuário ou null em caso de erro</returns>
    //Task<UserInfo?> UnbanUserAsync(UnbanUserRequest request, Guid adminUserId);
    
    /// <summary>
    /// Ativa ou desativa um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="isActive">Estado de ativação</param>
    /// <returns>Dados atualizados do usuário ou null em caso de erro</returns>
    //Task<UserInfo?> SetUserActiveStatusAsync(Guid userId, bool isActive);
    
    /// <summary>
    /// Altera a senha de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="request">Requisição com os dados de alteração</param>
    /// <returns>Verdadeiro se a senha foi alterada com sucesso</returns>
    //Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    
    /// <summary>
    /// Inicia o processo de recuperação de senha
    /// </summary>
    /// <param name="request">Requisição com email do usuário</param>
    /// <returns>Verdadeiro se o processo foi iniciado com sucesso</returns>
    //Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
    
    /// <summary>
    /// Redefine a senha de um usuário após o processo de recuperação
    /// </summary>
    /// <param name="request">Requisição com os dados de redefinição</param>
    /// <returns>Verdadeiro se a senha foi redefinida com sucesso</returns>
    //Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}