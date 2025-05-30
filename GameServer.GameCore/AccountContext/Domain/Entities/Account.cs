using GameServer.GameCore.AccountContext.Domain.Enums;
using GameServer.GameCore.AccountContext.Domain.Events;
using GameServer.GameCore.AccountContext.Domain.Exceptions;
using GameServer.GameCore.AccountContext.Domain.ValueObjects;

namespace GameServer.GameCore.AccountContext.Domain.Entities;

public class Account : Entity<long>
{
    #region Propriedades

    // Informações básicas da conta
    public UsernameVO Username { get; private set; } = default!;
    public EmailVO Email { get; private set; } = default!;
    public PasswordVO Password { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public AccountType AccountType { get; private set; } = AccountType.Player;

    // Informações de segurança
    public BanInfoVO BanInfo { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = default!;
    public LoginInfoVO LastLoginInfo { get; private set; } = default!;

    // Permissões
    private readonly List<RoleVO> _roles = new();
    public IReadOnlyCollection<RoleVO> Roles => _roles.AsReadOnly();

    #endregion

    #region Construtores

    public Account(UsernameVO username, EmailVO email, PasswordVO password)
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        IsActive = true;
        BanInfo = BanInfoVO.NotBanned();
        CreatedAt = DateTime.UtcNow;

        // Adiciona automaticamente a role padrão
        _roles.Add(RoleVO.Create("player"));

        // Adiciona evento de domínio
        AddDomainEvent(new AccountCreatedEvent(Id, Email, Username));
    }

    // Construtor sem parâmetros para o EF Core
    protected Account() { }

    #endregion

    #region Métodos de verificação de status

    public bool IsStaff() => AccountType == AccountType.Staff;
    public bool IsAdministrator() => AccountType == AccountType.Administrator;
    public bool HasRole(string roleName) => _roles.Any(r => r.Value.Equals(roleName, StringComparison.OrdinalIgnoreCase));

    #endregion

    #region Gerenciamento de estado da conta

    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        AddDomainEvent(new AccountActivatedEvent(Id, Username));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        AddDomainEvent(new AccountDeactivatedEvent(Id, Username));
    }

    public void UpdateBan(BanInfoVO banInfo)
    {
        if (banInfo.IsActive() && AccountType == AccountType.Administrator)
            throw new DomainException("Administradores não podem ser banidos");

        BanInfo = banInfo;
        AddDomainEvent(new AccountBanUpdatedEvent(Id, banInfo));
    }

    #endregion

    #region Autenticação e Sessão

    public void RecordSuccessfulLogin(LoginInfoVO loginInfo)
    {
        if (!IsActive)
            throw new DomainException("Conta inativa não pode realizar login");

        if (BanInfo.IsActive())
            throw new DomainException($"Conta banida até {BanInfo.ExpiresAt}. Motivo: {BanInfo.Reason}");

        LastLoginInfo = loginInfo;

        AddDomainEvent(new AccountLoggedIn(Id, Username, loginInfo));
    }

    public void RecordFailedLoginAttempt(LoginInfoVO loginInfo)
    {
        AddDomainEvent(new AccountLoginFailed(Id, Username, loginInfo));
    }

    public void RecordLogout()
    {
        AddDomainEvent(new AccountLoggedOut(Id));
    }

    #endregion

    #region Gerenciamento de dados da conta

    public void UpdateEmail(EmailVO newEmail)
    {
        var previousEmail = Email;
        Email = newEmail;

        AddDomainEvent(new AccountEmailUpdated(Id, previousEmail, newEmail));
    }

    public void UpdatePassword(PasswordVO newPassword)
    {
        Password = newPassword;
        AddDomainEvent(new AccountPasswordUpdated(Id));
    }

    #endregion

    #region Gerenciamento de roles e tipos de conta

    public void AddRole(RoleVO role)
    {
        if (_roles.Contains(role)) return;

        if (role.Value.Equals("admin", StringComparison.OrdinalIgnoreCase) &&
            AccountType != AccountType.Administrator)
            throw new DomainException("Apenas administradores podem ter a role admin");

        _roles.Add(role);
        AddDomainEvent(new AccountRoleAddedEvent(Id, role));
    }

    public void RemoveRole(RoleVO role)
    {
        if (!_roles.Contains(role)) return;

        _roles.Remove(role);
        AddDomainEvent(new AccountRoleRemovedEvent(Id, role));
    }

    public void PromoteToStaff()
    {
        var previousType = AccountType;
        if (previousType == AccountType.Staff) return;

        if (BanInfo.IsActive())
            throw new DomainException("Contas banidas não podem ser promovidas a Staff");

        if (!IsActive)
            throw new DomainException("Apenas contas ativas podem se tornar Staff");

        if (!HasMinimumRequirementsForStaff())
            throw new DomainException("Conta não possui requisitos mínimos para virar Staff");

        AccountType = AccountType.Staff;
        AddDomainEvent(new AccountTypeChangedEvent(Id, previousType, AccountType));
    }

    public void PromoteTo(AccountType newAccountType)
    {
        if (!CanBePromotedTo(newAccountType))
            throw new DomainException($"Não é possível promover a conta para {newAccountType}");

        var previousType = AccountType;
        AccountType = newAccountType;

        if (newAccountType == AccountType.Administrator && !HasRole("admin"))
            _roles.Add(RoleVO.Create("admin"));

        AddDomainEvent(new AccountTypeChangedEvent(Id, previousType, AccountType));
    }

    #endregion

    #region Verificações de permissão e promoção

    public bool HasPermissionTo(string action)
    {
        if (AccountType == AccountType.Administrator)
            return true;

        return action.ToLowerInvariant() switch
        {
            "moderate_chat" => Roles.Any(r => r.CanModerateChat()) || AccountType == AccountType.Staff,
            "manage_accounts" => Roles.Any(r => r.CanManageAccounts()),
            "view_reports" => HasRole("moderator") || HasRole("support") || IsStaff(),
            "access_vip_area" => HasRole("vip") || AccountType == AccountType.VIP || IsStaff(),
            "create_game_items" => HasRole("gamemaster") || AccountType == AccountType.Staff,
            "manage_servers" => IsAdministrator(),
            _ => false
        };
    }

    public bool CanBePromotedTo(AccountType newType)
    {
        if (newType <= AccountType)
            return false;

        if (BanInfo.IsActive())
            return false;

        if (!IsActive)
            return false;

        return newType switch
        {
            AccountType.VIP => true,
            AccountType.Staff => HasMinimumRequirementsForStaff(),
            AccountType.Administrator => HasRole("admin") && IsStaff(),
            _ => false
        };
    }

    private bool HasMinimumRequirementsForStaff()
    {
        return HasRole("moderator") || HasRole("support") || HasRole("gamemaster");
    }

    #endregion
}