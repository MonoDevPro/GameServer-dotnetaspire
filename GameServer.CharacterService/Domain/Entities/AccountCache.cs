namespace GameServer.CharacterService.Domain.Entities;

/// <summary>
/// Cache local de dados essenciais de contas de usuário
/// </summary>
public class AccountCache
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    private AccountCache() { }

    public AccountCache(Guid id, string email, bool isActive)
    {
        Id = id;
        Email = email;
        IsActive = isActive;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void Update(string email, bool isActive)
    {
        Email = email;
        IsActive = isActive;
        LastUpdatedAt = DateTime.UtcNow;
    }
}