namespace GameServer.AuthService.Service.Application.Models;

/// <summary>
/// Resultado do registro de um novo usuário
/// </summary>
public class UserRegistrationResult
{
    public bool Succeeded { get; set; }
    public UserDto? User { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}