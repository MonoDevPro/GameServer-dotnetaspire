
namespace GameServer.AccountService.Service.Application.Ports.In;

/// <summary>
/// Interface para o serviço de gerenciamento de permissões na camada de aplicação
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Obtém todas as permissões de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Lista de permissões do usuário</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);

    /// <summary>
    /// Adiciona uma permissão a um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="policyName">Nome da política (permissão)</param>
    /// <param name="description">Descrição da permissão</param>
    /// <returns>Verdadeiro se a permissão foi adicionada com sucesso</returns>
    Task<bool> AddPermissionToUserAsync(Guid userId, string policyName, string? description = null);

    /// <summary>
    /// Remove uma permissão de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="permissionId">ID da permissão</param>
    /// <returns>Verdadeiro se a permissão foi removida com sucesso</returns>
    Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId);

    /// <summary>
    /// Verifica se um usuário possui uma permissão específica
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="policyName">Nome da política (permissão)</param>
    /// <returns>Verdadeiro se o usuário possui a permissão</returns>
    Task<bool> UserHasPermissionAsync(Guid userId, string policyName);

    /// <summary>
    /// Obtém todos os papéis (roles) de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Lista de papéis do usuário</returns>
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);

    /// <summary>
    /// Adiciona um papel (role) a um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="roleName">Nome do papel</param>
    /// <returns>Verdadeiro se o papel foi adicionado com sucesso</returns>
    Task<bool> AddRoleToUserAsync(Guid userId, string roleName);

    /// <summary>
    /// Remove um papel (role) de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="roleName">Nome do papel</param>
    /// <returns>Verdadeiro se o papel foi removido com sucesso</returns>
    Task<bool> RemoveRoleFromUserAsync(Guid userId, string roleName);

    /// <summary>
    /// Verifica se um usuário possui um papel específico
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="roleName">Nome do papel</param>
    /// <returns>Verdadeiro se o usuário possui o papel</returns>
    Task<bool> UserHasRoleAsync(Guid userId, string roleName);

    /// <summary>
    /// Cria um novo papel (role) no sistema
    /// </summary>
    /// <param name="roleName">Nome do papel</param>
    /// <param name="description">Descrição do papel</param>
    /// <returns>Verdadeiro se o papel foi criado com sucesso</returns>
    Task<bool> CreateRoleAsync(string roleName, string description);

    /// <summary>
    /// Adiciona uma permissão a um papel
    /// </summary>
    /// <param name="roleName">Nome do papel</param>
    /// <param name="policyName">Nome da política (permissão)</param>
    /// <returns>Verdadeiro se a permissão foi adicionada com sucesso</returns>
    Task<bool> AddPermissionToRoleAsync(string roleName, string policyName);

    /// <summary>
    /// Remove uma permissão de um papel
    /// </summary>
    /// <param name="roleName">Nome do papel</param>
    /// <param name="policyName">Nome da política (permissão)</param>
    /// <returns>Verdadeiro se a permissão foi removida com sucesso</returns>
    Task<bool> RemovePermissionFromRoleAsync(string roleName, string policyName);
}