namespace GameServer.GameCore.AccountContext.Domain.Enums;

/// <summary>
/// Represents the different account types in the system
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Regular player account
    /// </summary>
    Player = 0,
    
    /// <summary>
    /// VIP account with additional benefits
    /// </summary>
    VIP = 1,
    
    /// <summary>
    /// Game master account with moderation privileges
    /// </summary>
    GameMaster = 2,
    
    /// <summary>
    /// Staff member account with additional privileges
    /// </summary>
    Staff = 3,
    
    /// <summary>
    /// Administrator account with full privileges
    /// </summary>
    Administrator = 4
}