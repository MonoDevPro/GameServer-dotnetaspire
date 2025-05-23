namespace GameServer.AccountService.AccountManagement.Domain.Enums;

/// <summary>
/// Status de banimento do usuário
/// </summary>
public enum BanStatus
{
    /// <summary>
    /// Não está banido
    /// </summary>
    NotBanned = 0,
    
    /// <summary>
    /// Banido temporariamente
    /// </summary>
    TemporaryBan = 1,
    
    /// <summary>
    /// Banido permanentemente
    /// </summary>
    PermanentBan = 2,
    
    /// <summary>
    /// Conta suspensa (medida temporária menos severa)
    /// </summary>
    Suspended = 3
}