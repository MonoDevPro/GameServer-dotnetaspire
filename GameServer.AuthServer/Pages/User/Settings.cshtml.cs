using System.ComponentModel.DataAnnotations;
using GameServer.AuthServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameServer.AuthServer.Pages.User;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ILogger<SettingsModel> _logger;
    private readonly IUserAuthenticationTypeService _authTypeService;
    private readonly IPasswordAuditService _passwordAuditService;

    public SettingsModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager,
        ILogger<SettingsModel> logger,
        IUserAuthenticationTypeService authTypeService,
        IPasswordAuditService passwordAuditService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _authTypeService = authTypeService;
        _passwordAuditService = passwordAuditService;
    }

    [BindProperty]
    public SecurityInputModel SecurityInput { get; set; } = new();

    [BindProperty]
    public NotificationInputModel NotificationInput { get; set; } = new();

    [BindProperty]
    public PrivacyInputModel PrivacyInput { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime? AccountCreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool HasLocalPassword { get; set; }
    public bool IsOAuthOnlyUser { get; set; }
    public List<string> ExternalLogins { get; set; } = new();

    public class SecurityInputModel
    {
        [Required(ErrorMessage = "A senha atual é obrigatória.")]
        [Display(Name = "Senha Atual")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
        [Display(Name = "Nova Senha")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "A confirmação da senha é obrigatória.")]
        [Display(Name = "Confirmar Nova Senha")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "A nova senha e a confirmação não coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Habilitar 2FA")]
        public bool TwoFactorEnabled { get; set; }
    }

    public class NotificationInputModel
    {
        [Display(Name = "Notificar sobre novos logins")]
        public bool EmailOnLogin { get; set; }

        [Display(Name = "Notificar sobre mudanças de senha")]
        public bool EmailOnPasswordChange { get; set; }

        [Display(Name = "Alertas de segurança")]
        public bool EmailOnSecurityAlert { get; set; }

        [Display(Name = "Manutenções do sistema")]
        public bool SystemMaintenance { get; set; }

        [Display(Name = "Atualizações do jogo")]
        public bool GameUpdates { get; set; }
    }

    public class PrivacyInputModel
    {
        [Display(Name = "Perfil público")]
        public bool ProfilePublic { get; set; }

        [Display(Name = "Mostrar status online")]
        public bool ShowOnlineStatus { get; set; }

        [Display(Name = "Permitir coleta de dados para melhorias")]
        public bool AllowAnalytics { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        // Carregar informações básicas do usuário
        UserId = user.Id.ToString();
        UserEmail = user.Email;

        // Carregar informações de autenticação
        HasLocalPassword = await _authTypeService.HasLocalPasswordAsync(user);
        IsOAuthOnlyUser = await _authTypeService.IsOAuthOnlyUserAsync(user);
        ExternalLogins = await _authTypeService.GetExternalLoginsAsync(user);

        // Simular datas (em uma implementação real, essas informações estariam no banco)
        AccountCreatedDate = DateTime.Now.AddDays(-Random.Shared.Next(30, 365));
        LastLoginDate = DateTime.Now.AddHours(-Random.Shared.Next(1, 24));

        // Carregar configurações atuais (simuladas - em uma implementação real, essas estariam em uma tabela de configurações)
        LoadUserSettings();

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateSecurityAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        // Verificar se o usuário tem permissão para alterar senha
        var hasLocalPassword = await _authTypeService.HasLocalPasswordAsync(user);
        if (!hasLocalPassword)
        {
            StatusMessage = "Usuários conectados apenas via OAuth não podem alterar a senha aqui. Gerencie sua senha no provedor de autenticação (GitHub).";
            return RedirectToPage();
        }

        if (!ModelState.IsValid)
        {
            await LoadUserInfo(user);
            return Page();
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Verificar senha atual
            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, SecurityInput.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                await _passwordAuditService.LogPasswordChangeAttemptAsync(user, false, ipAddress, userAgent);
                ModelState.AddModelError("SecurityInput.CurrentPassword", "Senha atual incorreta.");
                await LoadUserInfo(user);
                return Page();
            }

            // Alterar senha
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, SecurityInput.CurrentPassword, SecurityInput.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                var errors = string.Join("; ", changePasswordResult.Errors.Select(e => e.Description));
                await _passwordAuditService.LogPasswordChangeAsync(user, false, errors, ipAddress, userAgent);

                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadUserInfo(user);
                return Page();
            }

            // Atualizar security stamp para invalidar outras sessões
            await _userManager.UpdateSecurityStampAsync(user);

            // Log de sucesso
            await _passwordAuditService.LogPasswordChangeAsync(user, true, null, ipAddress, userAgent);

            StatusMessage = "Sua senha foi alterada com sucesso. Por segurança, você foi desconectado de outros dispositivos.";
            _logger.LogInformation("User {UserId} changed their password successfully", user.Id);

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", user.Id);
            StatusMessage = "Ocorreu um erro ao alterar sua senha. Tente novamente.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostUpdateNotificationsAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // Em uma implementação real, você salvaria essas configurações em uma tabela de configurações
            // Por agora, apenas log da operação
            _logger.LogInformation("User {UserId} updated notification settings: EmailOnLogin={EmailOnLogin}, EmailOnPasswordChange={EmailOnPasswordChange}, EmailOnSecurityAlert={EmailOnSecurityAlert}, SystemMaintenance={SystemMaintenance}, GameUpdates={GameUpdates}",
                user.Id, NotificationInput.EmailOnLogin, NotificationInput.EmailOnPasswordChange, NotificationInput.EmailOnSecurityAlert, NotificationInput.SystemMaintenance, NotificationInput.GameUpdates);

            StatusMessage = "Configurações de notificação atualizadas com sucesso.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings for user {UserId}", user.Id);
            StatusMessage = "Ocorreu um erro ao atualizar as configurações de notificação.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostUpdatePrivacyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // Em uma implementação real, você salvaria essas configurações em uma tabela de configurações
            // Por agora, apenas log da operação
            _logger.LogInformation("User {UserId} updated privacy settings: ProfilePublic={ProfilePublic}, ShowOnlineStatus={ShowOnlineStatus}, AllowAnalytics={AllowAnalytics}",
                user.Id, PrivacyInput.ProfilePublic, PrivacyInput.ShowOnlineStatus, PrivacyInput.AllowAnalytics);

            StatusMessage = "Configurações de privacidade atualizadas com sucesso.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating privacy settings for user {UserId}", user.Id);
            StatusMessage = "Ocorreu um erro ao atualizar as configurações de privacidade.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostLogoutAllDevicesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // Atualizar security stamp para invalidar todas as sessões
            await _userManager.UpdateSecurityStampAsync(user);

            // Fazer logout da sessão atual
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {UserId} logged out from all devices", user.Id);

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout all devices for user {UserId}", user.Id);
            StatusMessage = "Ocorreu um erro ao fazer logout de todos os dispositivos.";
            return RedirectToPage();
        }
    }

    private async Task LoadUserInfo(IdentityUser<Guid> user)
    {
        UserId = user.Id.ToString();
        UserEmail = user.Email;

        // Carregar informações de autenticação
        HasLocalPassword = await _authTypeService.HasLocalPasswordAsync(user);
        IsOAuthOnlyUser = await _authTypeService.IsOAuthOnlyUserAsync(user);
        ExternalLogins = await _authTypeService.GetExternalLoginsAsync(user);

        AccountCreatedDate = DateTime.Now.AddDays(-Random.Shared.Next(30, 365));
        LastLoginDate = DateTime.Now.AddHours(-Random.Shared.Next(1, 24));
        LoadUserSettings();
    }

    private void LoadUserSettings()
    {
        // Carregar configurações padrão (em uma implementação real, essas viriam do banco de dados)
        NotificationInput.EmailOnLogin = true;
        NotificationInput.EmailOnPasswordChange = true;
        NotificationInput.EmailOnSecurityAlert = true;
        NotificationInput.SystemMaintenance = false;
        NotificationInput.GameUpdates = true;

        PrivacyInput.ProfilePublic = false;
        PrivacyInput.ShowOnlineStatus = true;
        PrivacyInput.AllowAnalytics = true;

        SecurityInput.TwoFactorEnabled = false; // 2FA ainda não implementado
    }
}
