
namespace GameServer.AccountService.Service.Application.Ports.In;

/// <summary>
/// Interface para o serviço de registro de usuários na camada de aplicação
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Registra um novo usuário
    /// </summary>
    /// <param name="command">Requisição com os dados de registro</param>
    /// <param name="ipAddress">Endereço IP do cliente</param>
    /// <returns>Resposta com o resultado do registro</returns>
    //Task<RegisterResponse> RegisterUserAsync(AccountCommands command, string ipAddress);
    
    /// <summary>
    /// Confirma o email de um usuário
    /// </summary>
    /// <param name="request">Requisição com os dados de confirmação</param>
    /// <returns>Verdadeiro se a confirmação foi bem-sucedida</returns>
    //Task<bool> ConfirmRegistrationAsync(ConfirmRegistrationRequest request);
    
    /// <summary>
    /// Gera um novo token de confirmação de email
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Token gerado ou null em caso de erro</returns>
    //Task<string?> GenerateEmailConfirmationTokenAsync(Guid userId);
    
    /// <summary>
    /// Envia um email de confirmação para o usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="token">Token de confirmação</param>
    /// <returns>Verdadeiro se o email foi enviado com sucesso</returns>
    //Task<bool> SendEmailConfirmationAsync(Guid userId, string token);
}