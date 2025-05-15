using System.Security.Claims;
using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.In;
using GameServer.AuthService.Application.Ports.Out;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServer.AuthService.Infrastructure.Adapters.In.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IUserCache _cache;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        ITokenGenerator tokenGenerator,
        IUserCache cache,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _tokenGenerator = tokenGenerator;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Obtém os detalhes da conta do usuário autenticado
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyAccount()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse("Token inválido ou expirado."));
        }

        var account = await _accountService.GetAccountDetailsAsync(userId.Value);
        if (account == null)
        {
            return NotFound(new ErrorResponse("Conta não encontrada."));
        }

        return Ok(account);
    }

    /// <summary>
    /// Atualiza informações da conta do usuário autenticado
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyAccount([FromBody] UpdateAccountRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse("Token inválido ou expirado."));
        }

        try
        {
            var account = await _accountService.UpdateAccountAsync(userId.Value, request);
            if (account == null)
            {
                return NotFound(new ErrorResponse("Conta não encontrada."));
            }

            return Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Obtém os detalhes da conta de um usuário específico (apenas para admins)
    /// </summary>
    [HttpGet("{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(Guid userId)
    {
        var account = await _accountService.GetAccountDetailsAsync(userId);
        if (account == null)
        {
            return NotFound(new ErrorResponse("Conta não encontrada."));
        }

        return Ok(account);
    }

    /// <summary>
    /// Bane uma conta de usuário (apenas para admins)
    /// </summary>
    [HttpPost("{userId:guid}/ban")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BanAccount(Guid userId, [FromBody] BanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new ErrorResponse("O motivo do banimento é obrigatório."));
        }

        var result = await _accountService.BanAccountAsync(userId, request.Until, request.Reason);
        if (!result)
        {
            return BadRequest(new ErrorResponse("Não foi possível banir o usuário."));
        }

        return Ok();
    }

    /// <summary>
    /// Remove o banimento de uma conta de usuário (apenas para admins)
    /// </summary>
    [HttpPost("{userId:guid}/unban")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnbanAccount(Guid userId)
    {
        var result = await _accountService.UnbanAccountAsync(userId);
        if (!result)
        {
            return BadRequest(new ErrorResponse("Não foi possível desbanir o usuário."));
        }

        return Ok();
    }

    /// <summary>
    /// Ativa uma conta de usuário (apenas para admins)
    /// </summary>
    [HttpPost("{userId:guid}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateAccount(Guid userId)
    {
        var result = await _accountService.ActivateAccountAsync(userId);
        if (!result)
        {
            return BadRequest(new ErrorResponse("Não foi possível ativar a conta."));
        }

        return Ok();
    }

    /// <summary>
    /// Desativa uma conta de usuário (apenas para admins)
    /// </summary>
    [HttpPost("{userId:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateAccount(Guid userId)
    {
        var result = await _accountService.DeactivateAccountAsync(userId);
        if (!result)
        {
            return BadRequest(new ErrorResponse("Não foi possível desativar a conta."));
        }

        return Ok();
    }

    /// <summary>
    /// Limpa o cache para um usuário específico, forçando a recarga dos dados do repositório
    /// </summary>
    [HttpPost("{userId:guid}/clear-cache")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearUserCache(Guid userId)
    {
        var account = await _accountService.GetAccountDetailsAsync(userId);
        if (account == null)
        {
            return NotFound(new ErrorResponse("Usuário não encontrado."));
        }

        await _cache.RemoveUserAsync(userId);
        _logger.LogInformation("Cache do usuário {UserId} limpo por solicitação admin", userId);

        return Ok(new { Message = $"Cache do usuário {userId} foi limpo com sucesso" });
    }
    
    /// <summary>
    /// Limpa o cache do usuário atual
    /// </summary>
    [HttpPost("me/clear-cache")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearMyCache()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse("Token inválido ou expirado."));
        }

        await _cache.RemoveUserAsync(userId.Value);
        _logger.LogInformation("Cache do usuário {UserId} limpo por solicitação própria", userId.Value);

        return Ok(new { Message = "Seu cache foi limpo com sucesso" });
    }

    /// <summary>
    /// Promove um usuário para administrador (apenas para admins)
    /// </summary>
    [HttpPost("{userId:guid}/promote-to-admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PromoteToAdmin(Guid userId)
    {
        _logger.LogInformation("Solicitação para promover usuário {UserId} a administrador", userId);
        
        // Verifica se o usuário já é admin
        if (await _accountService.IsAdminAsync(userId))
        {
            return BadRequest(new ErrorResponse("O usuário já é administrador."));
        }
        
        var result = await _accountService.PromoteToAdminAsync(userId);
        if (!result)
        {
            return NotFound(new ErrorResponse("Usuário não encontrado."));
        }
        
        // Limpa o cache do usuário para refletir a mudança quando o token for renovado
        await _cache.RemoveUserAsync(userId);

        return Ok(new { Message = "Usuário promovido a administrador com sucesso."});
    }

    /// <summary>
    /// Remove os privilégios de administrador de um usuário (apenas para admins)
    /// </summary>
    [HttpPost("{userId:guid}/demote-from-admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DemoteFromAdmin(Guid userId)
    {
        _logger.LogInformation("Solicitação para remover privilégios de administrador do usuário {UserId}", userId);
        
        // Impedir que um admin remova seus próprios privilégios de admin
        var currentUserId = GetUserIdFromToken();
        if (currentUserId == userId)
        {
            return BadRequest(new ErrorResponse("Você não pode remover seus próprios privilégios de administrador."));
        }
        
        // Verifica se o usuário é admin
        if (!await _accountService.IsAdminAsync(userId))
        {
            return BadRequest(new ErrorResponse("O usuário não é administrador."));
        }
        
        var result = await _accountService.DemoteFromAdminAsync(userId);
        if (!result)
        {
            return NotFound(new ErrorResponse("Usuário não encontrado."));
        }
        
        // Limpa o cache do usuário para refletir a mudança quando o token for renovado
        await _cache.RemoveUserAsync(userId);
        
        return Ok(new { Message = "Privilégios} de administrador removidos com sucesso."});
    }

    /// <summary>
    /// Verifica se um usuário é administrador (apenas para admins)
    /// </summary>
    [HttpGet("{userId:guid}/is-admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IsAdmin(Guid userId)
    {
        var isAdmin = await _accountService.IsAdminAsync(userId);
        return Ok(isAdmin);
    }

    private Guid? GetUserIdFromToken()
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        return _tokenGenerator.GetUserIdFromToken(token);
    }
}

