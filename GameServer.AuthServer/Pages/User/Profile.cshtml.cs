using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GameServer.AuthServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameServer.AuthServer.Pages.User;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ILogger<ProfileModel> _logger;
    private readonly IUserAuthenticationTypeService _authTypeService;

    public ProfileModel(
        UserManager<IdentityUser<Guid>> userManager,
        ILogger<ProfileModel> logger,
        IUserAuthenticationTypeService authTypeService)
    {
        _userManager = userManager;
        _logger = logger;
        _authTypeService = authTypeService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public DateTime? AccountCreatedDate { get; set; }
    public List<Claim> UserClaims { get; set; } = new();
    public bool HasLocalPassword { get; set; }
    public bool IsOAuthOnlyUser { get; set; }
    public List<string> ExternalLogins { get; set; } = new();

    public class InputModel
    {
        [Display(Name = "Nome")]
        [StringLength(50, ErrorMessage = "O {0} deve ter no máximo {1} caracteres.")]
        public string? FirstName { get; set; }

        [Display(Name = "Sobrenome")]
        [StringLength(50, ErrorMessage = "O {0} deve ter no máximo {1} caracteres.")]
        public string? LastName { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Telefone")]
        [Phone(ErrorMessage = "Número de telefone inválido.")]
        [StringLength(20, ErrorMessage = "O {0} deve ter no máximo {1} caracteres.")]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        // Carregar dados do usuário
        Input.Email = user.Email ?? string.Empty;
        Input.PhoneNumber = user.PhoneNumber;

        // Carregar claims para extrair nome e sobrenome
        var claims = await _userManager.GetClaimsAsync(user);
        UserClaims = claims.ToList();

        Input.FirstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        Input.LastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;

        // Carregar informações de autenticação
        HasLocalPassword = await _authTypeService.HasLocalPasswordAsync(user);
        IsOAuthOnlyUser = await _authTypeService.IsOAuthOnlyUserAsync(user);
        ExternalLogins = await _authTypeService.GetExternalLoginsAsync(user);

        // Simular data de criação da conta baseada no ID
        AccountCreatedDate = DateTime.Now.AddDays(-Random.Shared.Next(30, 365));

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            // Recarregar dados se o modelo não for válido
            var claims = await _userManager.GetClaimsAsync(user);
            UserClaims = claims.ToList();
            AccountCreatedDate = DateTime.Now.AddDays(-Random.Shared.Next(30, 365));
            return Page();
        }

        try
        {
            // Atualizar telefone
            if (Input.PhoneNumber != user.PhoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Erro inesperado ao atualizar número de telefone.";
                    return RedirectToPage();
                }
            }

            // Atualizar claims de nome
            var existingClaims = await _userManager.GetClaimsAsync(user);

            // Remover claims existentes de nome
            var givenNameClaim = existingClaims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName);
            var surnameClaim = existingClaims.FirstOrDefault(c => c.Type == ClaimTypes.Surname);
            var nameClaim = existingClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

            if (givenNameClaim != null)
                await _userManager.RemoveClaimAsync(user, givenNameClaim);
            if (surnameClaim != null)
                await _userManager.RemoveClaimAsync(user, surnameClaim);
            if (nameClaim != null)
                await _userManager.RemoveClaimAsync(user, nameClaim);

            // Adicionar novos claims
            var claimsToAdd = new List<Claim>();

            if (!string.IsNullOrEmpty(Input.FirstName))
                claimsToAdd.Add(new Claim(ClaimTypes.GivenName, Input.FirstName));

            if (!string.IsNullOrEmpty(Input.LastName))
                claimsToAdd.Add(new Claim(ClaimTypes.Surname, Input.LastName));

            if (!string.IsNullOrEmpty(Input.FirstName) || !string.IsNullOrEmpty(Input.LastName))
            {
                var fullName = $"{Input.FirstName} {Input.LastName}".Trim();
                claimsToAdd.Add(new Claim(ClaimTypes.Name, fullName));
            }

            if (claimsToAdd.Any())
            {
                var addClaimsResult = await _userManager.AddClaimsAsync(user, claimsToAdd);
                if (!addClaimsResult.Succeeded)
                {
                    StatusMessage = "Erro inesperado ao atualizar informações do perfil.";
                    return RedirectToPage();
                }
            }

            StatusMessage = "Seu perfil foi atualizado com sucesso.";
            _logger.LogInformation("User {UserId} updated their profile", user.Id);

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for user {UserId}", user.Id);
            StatusMessage = "Ocorreu um erro ao atualizar seu perfil. Tente novamente.";
            return RedirectToPage();
        }
    }
}
