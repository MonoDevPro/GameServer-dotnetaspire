namespace GameServer.AuthService.Service.Application.Models;

/// <summary>
/// Resultado da validação de um usuário
/// </summary>
public class UserValidationResult
{
    /// <summary>
    /// Indica se a validação foi bem-sucedida
    /// </summary>
    public bool Succeeded { get; set; }
    
    /// <summary>
    /// Usuário associado, se a validação foi bem-sucedida
    /// </summary>
    public UserDto? User { get; set; }

    /// <summary>
    /// Mensagem de erro, se houver
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Descrição do erro, se houver
    /// </summary>
    public string? ErrorDescription { get; set; }
}