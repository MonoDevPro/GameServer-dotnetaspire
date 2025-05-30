using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameServer.AuthServer.Areas.Identity.Pages.Account;

public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly IUserStore<IdentityUser<Guid>> _userStore;
    private readonly IUserEmailStore<IdentityUser<Guid>> _emailStore;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        UserManager<IdentityUser<Guid>> userManager,
        IUserStore<IdentityUser<Guid>> userStore,
        ILogger<ExternalLoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string ProviderDisplayName { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;
    }

    public IActionResult OnGet() => RedirectToPage("./Login");

    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        // Request a redirect to the external login provider.
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        if (remoteError != null)
        {
            ErrorMessage = $"Erro do provedor externo: {remoteError}";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Erro ao carregar informações de login externo.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name, info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        // If the user does not have an account, then ask the user to create an account.
        ReturnUrl = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;

        var emailClaim = info.Principal.FindFirst(ClaimTypes.Email);
        if (emailClaim != null && !string.IsNullOrEmpty(emailClaim.Value))
        {
            Input = new InputModel
            {
                Email = emailClaim.Value
            };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        // Get the information about the user from the external login provider
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Erro ao carregar informações de login externo durante confirmação.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (ModelState.IsValid)
        {
            // Check if a user with this email already exists
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                // User with this email already exists, let's try to add the external login to the existing account
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    _logger.LogInformation("External login {Provider} added to existing user {Email}.", info.LoginProvider, Input.Email);
                    await _signInManager.SignInAsync(existingUser, isPersistent: false, info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    // If adding the login failed, show an error
                    ModelState.AddModelError(string.Empty, $"Uma conta com o email {Input.Email} já existe. Faça login com sua conta existente e vincule o {info.ProviderDisplayName ?? info.LoginProvider} nas configurações de perfil.");
                }
            }
            else
            {
                // Create a new user
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        // Add claims to the user - handle multiple possible claim types
                        var nameClaim = info.Principal.FindFirst(ClaimTypes.Name) ??
                                       info.Principal.FindFirst(ClaimTypes.GivenName) ??
                                       info.Principal.FindFirst("urn:github:username") ??
                                       info.Principal.FindFirst("name");

                        if (nameClaim != null && !string.IsNullOrEmpty(nameClaim.Value))
                        {
                            // If it's not already a GivenName claim, create one
                            if (nameClaim.Type != ClaimTypes.GivenName)
                            {
                                await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.GivenName, nameClaim.Value));
                            }
                            else
                            {
                                await _userManager.AddClaimAsync(user, nameClaim);
                            }
                        }

                        // Handle surname - GitHub usually doesn't provide this, so we'll skip it
                        var surnameClaim = info.Principal.FindFirst(ClaimTypes.Surname);
                        if (surnameClaim != null && !string.IsNullOrEmpty(surnameClaim.Value))
                        {
                            await _userManager.AddClaimAsync(user, surnameClaim);
                        }

                        // Add GitHub-specific claims
                        var githubUsernameClaim = info.Principal.FindFirst("urn:github:login");
                        if (githubUsernameClaim != null && !string.IsNullOrEmpty(githubUsernameClaim.Value))
                        {
                            await _userManager.AddClaimAsync(user, githubUsernameClaim);
                        }

                        var githubAvatarClaim = info.Principal.FindFirst("urn:github:avatar");
                        if (githubAvatarClaim != null && !string.IsNullOrEmpty(githubAvatarClaim.Value))
                        {
                            await _userManager.AddClaimAsync(user, githubAvatarClaim);
                        }

                        // Include the access token in the properties
                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
        ReturnUrl = returnUrl;
        return Page();
    }

    private IdentityUser<Guid> CreateUser()
    {
        try
        {
            return Activator.CreateInstance<IdentityUser<Guid>>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser<Guid>)}'. " +
                $"Ensure that '{nameof(IdentityUser<Guid>)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
        }
    }

    private IUserEmailStore<IdentityUser<Guid>> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }
        return (IUserEmailStore<IdentityUser<Guid>>)_userStore;
    }
}
