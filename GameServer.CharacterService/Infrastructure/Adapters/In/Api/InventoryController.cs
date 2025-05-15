using GameServer.CharacterService.Application.Models;
using GameServer.CharacterService.Application.Ports.In;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServer.CharacterService.Infrastructure.Adapters.In.Api;

[ApiController]
[Route("api/characters/{characterId:guid}/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryUseCases _inventoryUseCases;
    private readonly ICharacterUseCases _characterUseCases;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryUseCases inventoryUseCases,
        ICharacterUseCases characterUseCases,
        ILogger<InventoryController> logger)
    {
        _inventoryUseCases = inventoryUseCases;
        _characterUseCases = characterUseCases;
        _logger = logger;
    }

    /// <summary>
    /// Obtém o inventário completo de um personagem
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventory(Guid characterId)
    {
        try
        {
            // Verificar se o personagem existe e pertence ao usuário autenticado
            await ValidateCharacterAccess(characterId);
            
            var response = await _inventoryUseCases.GetInventory(characterId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personagem não encontrado: {CharacterId}", characterId);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acesso não autorizado ao inventário: {CharacterId}, {Message}", characterId, ex.Message);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter inventário do personagem {CharacterId}: {Message}", characterId, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao obter inventário", ex.Message));
        }
    }

    /// <summary>
    /// Adiciona um item ao inventário do personagem
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(Guid characterId, [FromBody] AddItemRequest request)
    {
        try
        {
            // Verificar se o personagem existe e pertence ao usuário autenticado
            await ValidateCharacterAccess(characterId);
            
            var item = await _inventoryUseCases.AddItem(characterId, request);
            return CreatedAtAction(nameof(GetInventory), new { characterId }, item);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personagem não encontrado: {CharacterId}", characterId);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acesso não autorizado ao inventário: {CharacterId}, {Message}", characterId, ex.Message);
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Dados inválidos para adição de item: {Message}", ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao adicionar item: {Message}", ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar item ao inventário do personagem {CharacterId}: {Message}", characterId, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao adicionar item ao inventário", ex.Message));
        }
    }

    /// <summary>
    /// Remove um item do inventário do personagem
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveItem(Guid characterId, [FromBody] RemoveItemRequest request)
    {
        try
        {
            // Verificar se o personagem existe e pertence ao usuário autenticado
            await ValidateCharacterAccess(characterId);
            
            var result = await _inventoryUseCases.RemoveItem(characterId, request);
            
            if (result)
            {
                return NoContent();
            }
            
            return BadRequest(new ErrorResponse("Falha ao remover item do inventário"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personagem não encontrado: {CharacterId}", characterId);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acesso não autorizado ao inventário: {CharacterId}, {Message}", characterId, ex.Message);
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Dados inválidos para remoção de item: {Message}", ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover item do inventário do personagem {CharacterId}: {Message}", characterId, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao remover item do inventário", ex.Message));
        }
    }

    /// <summary>
    /// Equipa um item
    /// </summary>
    [HttpPut("equip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EquipItem(Guid characterId, [FromBody] EquipItemRequest request)
    {
        try
        {
            // Verificar se o personagem existe e pertence ao usuário autenticado
            await ValidateCharacterAccess(characterId);
            
            var result = await _inventoryUseCases.EquipItem(characterId, request);
            
            if (result)
            {
                return NoContent();
            }
            
            return BadRequest(new ErrorResponse("Falha ao equipar item"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personagem não encontrado: {CharacterId}", characterId);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acesso não autorizado ao inventário: {CharacterId}, {Message}", characterId, ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao equipar item: {Message}", ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao equipar item para personagem {CharacterId}: {Message}", characterId, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao equipar item", ex.Message));
        }
    }

    /// <summary>
    /// Desequipa um item
    /// </summary>
    [HttpPut("unequip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnequipItem(Guid characterId, [FromBody] UnequipItemRequest request)
    {
        try
        {
            // Verificar se o personagem existe e pertence ao usuário autenticado
            await ValidateCharacterAccess(characterId);
            
            var result = await _inventoryUseCases.UnequipItem(characterId, request);
            
            if (result)
            {
                return NoContent();
            }
            
            return BadRequest(new ErrorResponse("Falha ao desequipar item"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personagem não encontrado: {CharacterId}", characterId);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acesso não autorizado ao inventário: {CharacterId}, {Message}", characterId, ex.Message);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desequipar item para personagem {CharacterId}: {Message}", characterId, ex.Message);
            return StatusCode(500, new ErrorResponse("Erro ao desequipar item", ex.Message));
        }
    }

    /// <summary>
    /// Valida se o usuário autenticado tem acesso ao personagem
    /// </summary>
    private async Task ValidateCharacterAccess(Guid characterId)
    {
        try
        {
            var character = await _characterUseCases.GetCharacterById(characterId);
            
            // Se chegamos aqui, o personagem existe
            // O serviço de aplicação já verifica se o personagem pertence ao usuário autenticado
        }
        catch (KeyNotFoundException ex)
        {
            throw; // Repassar a exceção
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar acesso ao personagem {CharacterId}: {Message}", characterId, ex.Message);
            throw new UnauthorizedAccessException("Não foi possível verificar o acesso ao personagem");
        }
    }
}