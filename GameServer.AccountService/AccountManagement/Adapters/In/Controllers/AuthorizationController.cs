using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.Entities;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;
using GameServer.AccountService.AccountManagement.Ports.Out.Security;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace GameServer.AccountService.AccountManagement.Adapters.In.Controllers;

[ApiController]
public class AuthorizationController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPasswordHashService _passwordHashService;
    
    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPasswordHashService passwordHashService
        )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _passwordHashService = passwordHashService;
    }
    
    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        
        if (request is null)
            throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");
        
        if (request.IsPasswordGrantType())
        {
            
            var passwordVO = PasswordVO.Create(request.Password, _passwordHashService);
            
            // 1) Valide credenciais:
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, passwordVO.Hash))
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            
            // 2) Crie claims principal:
            var principal = await _signInManager.CreateUserPrincipalAsync(user);

            // 3) Configure scopes
            //principal.SetScopes(new[] { OpenIddictConstants.Permissions.Scopes.Profile, OpenIddictConstants.Permissions.Scopes.Email, OpenIddictConstants.Permissions.Scopes.Roles });

            // 4) Retorne o token
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }
}
