using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameServer.AuthServer.Pages.Connect;

public class SignInModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public SignInModel(SignInManager<IdentityUser<Guid>> signInManager)
    {
        _signInManager = signInManager;
    }

    public string? ReturnUrl { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        // Check if user is already authenticated
        if (_signInManager.IsSignedIn(User))
        {
            return LocalRedirect(returnUrl ?? "~/");
        }

        // Redirect to the Identity login page
        return RedirectToPage("/Identity/Account/Login", new { area = "", returnUrl });
    }

    public IActionResult OnPost(string? returnUrl = null)
    {
        // Redirect to the Identity login page for POST requests too
        return RedirectToPage("/Identity/Account/Login", new { area = "", returnUrl });
    }
}
