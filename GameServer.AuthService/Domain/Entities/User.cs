namespace GameServer.AuthService.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = null!; // O construtor público sempre inicializa
    public string Email { get; private set; } = null!; // O construtor público sempre inicializa
    public string PasswordHash { get; private set; } = null!; // O construtor público sempre inicializa
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsBanned { get; private set; }
    public DateTime? BannedUntil { get; private set; }
    public string? BanReason { get; private set; }
    public bool IsAdmin { get; private set; } // Propriedade para controlar se o usuário é admin

    #pragma warning disable CS8618 // Construtor usado apenas pelo EF Core
    private User()
    {
        // Para o EF Core
    }
    #pragma warning restore CS8618

    public User(string username, string email, string passwordHash, bool isAdmin = false)
    {
        Id = Guid.NewGuid();
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        IsBanned = false;
        IsAdmin = isAdmin; // Inicializa como não-admin por padrão
    }

    // Método para promover um usuário a administrador
    public void PromoteToAdmin()
    {
        IsAdmin = true;
    }

    // Método para remover privilégios de administrador
    public void DemoteFromAdmin()
    {
        IsAdmin = false;
    }

    public void Ban(DateTime until, string reason)
    {
        IsBanned = true;
        BannedUntil = until;
        BanReason = reason;
    }

    public void Unban()
    {
        IsBanned = false;
        BannedUntil = null;
        BanReason = null;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public bool CanLogin()
    {
        return IsActive && (!IsBanned || (BannedUntil.HasValue && BannedUntil.Value < DateTime.UtcNow));
    }

    public void UpdateEmail(string newEmail)
    {
        Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash ?? throw new ArgumentNullException(nameof(newPasswordHash));
    }
}

