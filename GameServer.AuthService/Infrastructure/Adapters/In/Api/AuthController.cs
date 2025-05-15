using GameServer.AuthService.Application.Models;
using GameServer.AuthService.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameServer.AuthService.Infrastructure.Adapters.In.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registra um novo usuário
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("login")]  // Aplicando rate limiting para registro também
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("Recebida solicitação de registro para usuário: {Username}", request.Username);
        
        var result = await _authService.RegisterAsync(request);
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(new ErrorResponse(result.Message));
    }

    /// <summary>
    /// Autentica um usuário
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("login")]  // Aplicando rate limiting específico para login
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Recebida solicitação de login para: {UsernameOrEmail}", request.UsernameOrEmail);
        
        // Validar os dados de entrada
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse("Nome de usuário/email e senha são obrigatórios."));
        }
        
        var result = await _authService.LoginAsync(request);
        if (result.Success)
        {
            return Ok(result);
        }
        
        // Usar Unauthorized para falhas de autenticação ao invés de BadRequest
        return Unauthorized(new ErrorResponse(result.Message));
    }

    /// <summary>
    /// Valida um token JWT
    /// </summary>
    [HttpPost("validate-token")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateToken([FromBody] string token)
    {
        _logger.LogInformation("Validando token");
        var isValid = await _authService.ValidateTokenAsync(token);
        return Ok(isValid);
    }
}

