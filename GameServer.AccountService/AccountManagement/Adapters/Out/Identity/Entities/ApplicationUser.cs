using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Identity.Entities;

/// <summary>
/// Representa um usuário no sistema do MMORPG.
/// Estende o Identity com campos específicos para o contexto de jogos online.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Nome do usuário
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Sobrenome do usuário
    /// </summary>
    public string LastName { get; set; } = null!;
    
    /// <summary>
    /// Lista de roles do usuário (para serialização)
    /// </summary>
    [NotMapped]
    public List<string>? Roles { get; set; }
    
    /// <summary>
    /// Data de expiração da assinatura premium/VIP
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }

    /// <summary>
    /// Token de autenticação de dois fatores
    /// </summary>
    public string? TwoFactorToken { get; set; }
    
    /// <summary>
    /// Indica se a autenticação de dois fatores está habilitada
    /// </summary>
    public override bool TwoFactorEnabled { get; set; }
    
    /// <summary>
    /// Indica se o usuário concordou com os termos de serviço
    /// </summary>
    public bool AcceptedTermsOfService { get; set; }
    
    /// <summary>
    /// Data em que o usuário aceitou os termos de serviço
    /// </summary>
    public DateTime? TermsOfServiceAcceptedAt { get; set; }
}