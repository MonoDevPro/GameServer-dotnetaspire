using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.Entities;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Models;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;
using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Models;
using GameServer.AccountService.AccountManagement.Application.DTO;
using GameServer.AccountService.AccountManagement.Ports.In;
using GameServer.AccountService.AccountManagement.Ports.Out.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace GameServer.AccountService.AccountManagement.Adapters.In.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;
    //private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    //private readonly IAccountIdentitySyncService _accountIdentitySyncService;

    //private readonly UserManager<ApplicationUser> _userManager;
    //private readonly SignInManager<ApplicationUser> _signInManager;
    //private readonly IOpenIddictTokenManager _tokenManager; // ou via HttpContext

    public AccountController(
        ICommandBus commandBus,
        IQueryBus queryBus
        //IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory
        //UserManager<ApplicationUser> userManager,
        //SignInManager<ApplicationUser> signInManager,
        //IOpenIddictTokenManager tokenManager
        )
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
        //_userClaimsPrincipalFactory = userClaimsPrincipalFactory;

        //_userManager = userManager;
        //_signInManager = signInManager;
        //_tokenManager = tokenManager;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _commandBus.SendAsync<RegisterCommand, ResultCommand>(command);
        return result.IsSuccess
            ? Ok(result)
            : BadRequest(result.ErrorMessage);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(long id)
    {
        var query = new GetAccountByIdQuery(id);
        var result = await _queryBus.SendAsync<GetAccountByIdQuery, AccountDto>(query);
        return result.Value != null ? Ok(result) : NotFound();
    }

    [HttpPost("login")]
    [Authorize]
    public async Task<IActionResult> Login([FromBody] AuthenticateCommand command)
    {
        var result = await _commandBus.SendAsync<AuthenticateCommand, ResultCommand>(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result.ErrorMessage);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var result = await _commandBus.SendAsync<LogoutCommand, ResultCommand>(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result.ErrorMessage);
    }
}