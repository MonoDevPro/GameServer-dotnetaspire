using System.Security.Claims;
using GameServer.CharacterService.Application.Ports.In;
using GameServer.GameCore.Character.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameServer.CharacterService.Infrastructure.Adapters.In.Api;

[ApiController]
[Route("api/characters")]
[Authorize]
public class CharactersController : ControllerBase
{
    private readonly ICharacterUseCases _characterUseCases;
    private readonly ILogger<CharactersController> _logger;

    public CharactersController(
        ICharacterUseCases characterUseCases,
        ILogger<CharactersController> logger)
    {
        _characterUseCases = characterUseCases;
        _logger = logger;
    }

    /// <summary>
    /// Obtém todos os personagens da conta do usuário autenticado
    /// </summary>
    [HttpGet]
    [EnableRateLimiting("character-get")]  // Aplicando rate limiting
    [ProducesResponseType(typeof(CharacterListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCharacters()
    {
        var accountId = GetAccountIdFromToken();

        if (accountId == Guid.Empty)
        {
            return Unauthorized(new ErrorResponse("ID de conta inválido no token"));
        }

        try
        {
            var response = await _characterUseCases.GetCharactersByAccount(accountId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter personagens da conta {AccountId}: {Message}", accountId, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao obter personagens", ex.Message));
        }
    }

    /// <summary>
    /// Obtém um personagem específico pelo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [EnableRateLimiting("character-get")]  // Aplicando rate limiting
    [ProducesResponseType(typeof(CharacterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCharacter(Guid id)
    {
        var accountId = GetAccountIdFromToken();

        if (accountId == Guid.Empty)
        {
            return Unauthorized(new ErrorResponse("ID de conta inválido no token"));
        }

        try
        {
            var character = await _characterUseCases.GetCharacterById(id);
            
            // Verificar se o personagem pertence ao usuário autenticado
            if (character.Id != Guid.Empty && character.Id != id)
            {
                return Forbid();
            }
            
            return Ok(character);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personagem não encontrado: {CharacterId}", id);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter personagem {CharacterId}: {Message}", id, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao obter personagem", ex.Message));
        }
    }

    /// <summary>
    /// Cria um novo personagem
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("character-put")]  // Aplicando rate limiting
    [ProducesResponseType(typeof(CharacterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterRequest request)
    {
        var accountId = GetAccountIdFromToken();

        if (accountId == Guid.Empty)
        {
            return Unauthorized(new ErrorResponse("ID de conta inválido no token"));
        }

        try
        {
            var character = await _characterUseCases.CreateCharacter(accountId, request);
            return CreatedAtAction(nameof(GetCharacter), new { id = character.Id }, character);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Dados inválidos para criação de personagem: {Message}", ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao criar personagem: {Message}", ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar personagem: {Message}", ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao criar personagem", ex.Message));
        }
    }

    /// <summary>
    /// Atualiza um personagem existente
    /// </summary>
    [HttpPut("{id:guid}")]
    [EnableRateLimiting("character-put")]  // Aplicando rate limiting
    [ProducesResponseType(typeof(CharacterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCharacter(Guid id, [FromBody] UpdateCharacterRequest request)
    {
        var accountId = GetAccountIdFromToken();

        if (accountId == Guid.Empty)
        {
            return Unauthorized(new ErrorResponse("ID de conta inválido no token"));
        }

        try
        {
            // Primeiro verificamos se o personagem existe e pertence à conta do usuário
            var existingCharacter = await _characterUseCases.GetCharacterById(id);
            
            var character = await _characterUseCases.UpdateCharacter(id, request);
            return Ok(character);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personagem não encontrado para atualização: {CharacterId}", id);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Dados inválidos para atualização de personagem: {Message}", ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao atualizar personagem: {Message}", ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar personagem {CharacterId}: {Message}", id, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao atualizar personagem", ex.Message));
        }
    }

    /// <summary>
    /// Desativa um personagem
    /// </summary>
    [HttpPut("{id:guid}/deactivate")]
    [EnableRateLimiting("character-put")]  // Aplicando rate limiting
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCharacter(Guid id)
    {
        var accountId = GetAccountIdFromToken();

        if (accountId == Guid.Empty)
        {
            return Unauthorized(new ErrorResponse("ID de conta inválido no token"));
        }

        try
        {
            // Primeiro verificamos se o personagem existe e pertence à conta do usuário
            var existingCharacter = await _characterUseCases.GetCharacterById(id);
            
            var result = await _characterUseCases.DeactivateCharacter(id);
            
            if (result)
            {
                return NoContent();
            }
            
            return StatusCode(500, new ErrorResponse("Falha ao desativar personagem"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personagem não encontrado para desativação: {CharacterId}", id);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desativar personagem {CharacterId}: {Message}", id, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao desativar personagem", ex.Message));
        }
    }

    /// <summary>
    /// Ativa um personagem
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    [EnableRateLimiting("character-put")]  // Aplicando rate limiting
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateCharacter(Guid id)
    {
        var accountId = GetAccountIdFromToken();

        if (accountId == Guid.Empty)
        {
            return Unauthorized(new ErrorResponse("ID de conta inválido no token"));
        }

        try
        {
            // Primeiro verificamos se o personagem existe e pertence à conta do usuário
            var existingCharacter = await _characterUseCases.GetCharacterById(id);
            
            var result = await _characterUseCases.ActivateCharacter(id);
            
            if (result)
            {
                return NoContent();
            }
            
            return StatusCode(500, new ErrorResponse("Falha ao ativar personagem"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personagem não encontrado para ativação: {CharacterId}", id);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao ativar personagem: {Message}", ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ativar personagem {CharacterId}: {Message}", id, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao ativar personagem", ex.Message));
        }
    }

    private Guid GetAccountIdFromToken()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (accountIdClaim != null && Guid.TryParse(accountIdClaim.Value, out Guid accountId))
        {
            return accountId;
        }

        return Guid.Empty;
    }
}