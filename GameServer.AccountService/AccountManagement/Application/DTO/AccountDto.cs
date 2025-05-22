using System.Security.Principal;

namespace GameServer.AccountService.AccountManagement.Application.DTO;

public class AccountDto
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public long Id { get; }
    
    /// <summary>
    /// Unique ID do usuário
    /// </summary>
    public Guid UniqueId { get; }
    
    /// <summary>
    /// Nome de usuário
    /// </summary>
    public string? UserName { get; }
    
    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; }
    
    /// <summary>
    /// Nome
    /// </summary>
    public string? FirstName { get; }
    
    /// <summary>
    /// Sobrenome
    /// </summary>
    public string? LastName { get; }

    /// <summary>
    /// Claims Principal
    /// </summary>
    public IPrincipal? ClaimsPrincipal { get; }
    
    public AccountDto(
        long id, 
        Guid uniqueId, 
        string? userName, 
        string? email, 
        string? firstName, 
        string? lastName,
        IPrincipal? claimsPrincipal = null)
    {
        Id = id;
        UniqueId = uniqueId;
        UserName = userName;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        ClaimsPrincipal = claimsPrincipal;
    }
}