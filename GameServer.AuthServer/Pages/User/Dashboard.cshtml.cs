using System.Security.Claims;
using GameServer.AuthServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameServer.AuthServer.Pages.User;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ILogger<DashboardModel> _logger;
    private readonly IUserAuthenticationTypeService _authTypeService;

    public DashboardModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager,
        ILogger<DashboardModel> logger,
        IUserAuthenticationTypeService authTypeService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _authTypeService = authTypeService;
    }

    [BindProperty]
    public string? UserEmail { get; set; }

    [BindProperty]
    public DateTime? LastLogin { get; set; }

    [BindProperty]
    public int LoginCount { get; set; }

    [BindProperty]
    public int DaysActive { get; set; }

    [BindProperty]
    public List<Claim> UserClaims { get; set; } = new();

    [BindProperty]
    public string? CurrentIp { get; set; }

    [BindProperty]
    public string? UserAgent { get; set; }

    [BindProperty]
    public DateTime? SessionStart { get; set; }

    public bool HasLocalPassword { get; set; }
    public bool IsOAuthOnlyUser { get; set; }
    public List<string> ExternalLogins { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        // Carregar informações do usuário
        UserEmail = user.Email;

        // Carregar informações de autenticação
        HasLocalPassword = await _authTypeService.HasLocalPasswordAsync(user);
        IsOAuthOnlyUser = await _authTypeService.IsOAuthOnlyUserAsync(user);
        ExternalLogins = await _authTypeService.GetExternalLoginsAsync(user);

        // Simular estatísticas (em uma implementação real, você salvaria essas informações no banco)
        LastLogin = DateTime.Now.AddHours(-2); // Simular último login
        LoginCount = Random.Shared.Next(10, 100); // Simular contagem de logins
        DaysActive = Random.Shared.Next(30, 365); // Simular dias ativos

        // Carregar claims do usuário
        UserClaims = (await _userManager.GetClaimsAsync(user)).ToList();

        // Informações da sessão atual
        CurrentIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        UserAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        SessionStart = DateTime.Now.AddMinutes(-Random.Shared.Next(5, 60)); // Simular início da sessão

        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAllDevicesAsync()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Atualizar o security stamp para invalidar todas as sessões
            await _userManager.UpdateSecurityStampAsync(user);

            // Fazer logout da sessão atual
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {UserId} logged out from all devices", user.Id);

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout all devices");
            return RedirectToPage();
        }
    }
}
