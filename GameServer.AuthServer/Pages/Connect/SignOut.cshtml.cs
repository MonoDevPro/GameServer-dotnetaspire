using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameServer.AuthServer.Pages.Connect;

public class SignOutModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public SignOutModel(SignInManager<IdentityUser<Guid>> signInManager)
    {
        _signInManager = signInManager;
    }

    public IActionResult OnGet() => RedirectToPage("/Identity/Account/Logout", new { area = "" });

    public async Task<IActionResult> OnPost(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();

        if (returnUrl != null && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }
}
