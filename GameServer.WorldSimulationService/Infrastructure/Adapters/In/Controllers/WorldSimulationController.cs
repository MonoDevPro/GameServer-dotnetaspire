using System.Numerics;
using GameServer.WorldService.Domain.Entities;
using GameServer.WorldSimulationService.Application.Ports.In;
using GameServer.WorldSimulationService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServer.WorldSimulationService.Infrastructure.Adapters.In.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class WorldSimulationController : ControllerBase
{
    private readonly IWorldSimulationUseCase _worldSimulationUseCase;
    private readonly ILogger<WorldSimulationController> _logger;

    public WorldSimulationController(
        IWorldSimulationUseCase worldSimulationUseCase,
        ILogger<WorldSimulationController> logger)
    {
        _worldSimulationUseCase = worldSimulationUseCase ?? throw new ArgumentNullException(nameof(worldSimulationUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // World Management endpoints
    
    [HttpPost("worlds")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorldDto>> CreateWorld([FromBody] CreateWorldDto createWorldDto)
    {
        try
        {
            var world = await _worldSimulationUseCase.CreateNewWorldAsync(
                createWorldDto.Name,
                createWorldDto.Description);

            if (world == null)
                return BadRequest("Failed to create world.");

            var worldDto = new WorldDto
            {
                WorldId = world.Id,
                Name = world.Name,
                Description = world.Description,
                IsActive = world.IsActive,
                CreatedAt = world.CreatedAt
            };

            return CreatedAtAction(nameof(GetWorldById), new { worldId = world.Id }, worldDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating world");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error creating world");
        }
    }

    [HttpGet("worlds/{worldId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorldDto>> GetWorldById(long worldId)
    {
        try
        {
            var world = await _worldSimulationUseCase.GetWorldByIdAsync(worldId);
            if (world == null)
            {
                return NotFound($"World with ID {worldId} not found");
            }

            var worldDto = new WorldDto
            {
                WorldId = world.Id,
                Name = world.Name,
                Description = world.Description,
                IsActive = world.IsActive,
                CreatedAt = world.CreatedAt
            };

            return Ok(worldDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving world with ID {WorldId}", worldId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving world");
        }
    }

    // Region Management endpoints
    
    [HttpPost("worlds/{worldId}/regions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegionDto>> AddRegionToWorld(
        long worldId, 
        [FromBody] CreateRegionDto createRegionDto)
    {
        try
        {
            var region = await _worldSimulationUseCase.AddRegionToWorldAsync(
                worldId,
                createRegionDto.Name,
                createRegionDto.Description,
                createRegionDto.Width,
                createRegionDto.Height,
                createRegionDto.WorldPosition);

            if (region == null)
            {
                return NotFound($"World with ID {worldId} not found");
            }

            var regionDto = new RegionDto
            {
                RegionId = region.Id,
                Name = region.Name,
                Description = region.Description,
                Width = region.Width,
                Height = region.Height,
                WorldPosition = region.WorldPosition,
                IsActive = region.IsActive,
                CreatedAt = region.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetRegionById),
                new { worldId = worldId, regionId = region.Id },
                regionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding region to world {WorldId}", worldId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error adding region to world");
        }
    }

    [HttpGet("worlds/{worldId}/regions/{regionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegionDto>> GetRegionById(long worldId, long regionId)
    {
        try
        {
            var region = await _worldSimulationUseCase.GetRegionByIdAsync(regionId);
            if (region == null)
            {
                return NotFound($"Region with ID {regionId} not found");
            }

            var regionDto = new RegionDto
            {
                RegionId = region.Id,
                Name = region.Name,
                Description = region.Description,
                Width = region.Width,
                Height = region.Height,
                WorldPosition = region.WorldPosition,
                IsActive = region.IsActive,
                CreatedAt = region.CreatedAt
            };

            return Ok(regionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving region with ID {RegionId}", regionId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving region");
        }
    }

    // Player Management endpoints
    
    [HttpPost("players")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PlayerDto>> AddPlayerToWorld([FromBody] AddPlayerDto addPlayerDto)
    {
        try
        {
            var player = await _worldSimulationUseCase.AddPlayerToWorldAsync(
                addPlayerDto.CharacterId,
                addPlayerDto.AccountId,
                addPlayerDto.Username,
                addPlayerDto.Name,
                addPlayerDto.Position);

            if (player == null)
            {
                return BadRequest("Failed to add player to world.");
            }

            var playerDto = new PlayerDto
            {
                PlayerId = player.Id,
                CharacterId = player.CharacterId,
                Username = player.Username,
                Name = player.Name,
                Health = player.Health,
                MaxHealth = player.MaxHealth,
                IsOnline = player.IsOnline,
                Position = player.Position,
                IsActive = player.IsActive
            };

            return CreatedAtAction(nameof(GetPlayerById), new { playerId = player.Id }, playerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding player to world");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error adding player to world");
        }
    }

    [HttpGet("players/{playerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PlayerDto>> GetPlayerById(long playerId)
    {
        try
        {
            var player = await _worldSimulationUseCase.GetPlayerByIdAsync(playerId);
            if (player == null)
            {
                return NotFound($"Player with ID {playerId} not found");
            }

            var playerDto = new PlayerDto
            {
                PlayerId = player.Id,
                CharacterId = player.CharacterId,
                Username = player.Username,
                Name = player.Name,
                Health = player.Health,
                MaxHealth = player.MaxHealth,
                IsOnline = player.IsOnline,
                Position = player.Position,
                IsActive = player.IsActive
            };

            return Ok(playerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving player with ID {PlayerId}", playerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving player");
        }
    }

    [HttpPut("players/{playerId}/position")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdatePlayerPosition(
        long playerId, 
        [FromBody] UpdatePositionDto updatePositionDto)
    {
        try
        {
            var success = await _worldSimulationUseCase.UpdatePlayerPositionAsync(
                playerId,
                updatePositionDto.Position);

            if (!success)
            {
                return NotFound($"Player with ID {playerId} not found or position update failed");
            }

            return Ok(new { message = "Position updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position for player {PlayerId}", playerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error updating player position");
        }
    }

    [HttpPut("players/{playerId}/rotation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdatePlayerRotation(
        long playerId, 
        [FromBody] UpdateRotationDto updateRotationDto)
    {
        try
        {
            var success = await _worldSimulationUseCase.UpdatePlayerRotationAsync(
                playerId,
                updateRotationDto.Rotation);

            if (!success)
            {
                return NotFound($"Player with ID {playerId} not found or rotation update failed");
            }

            return Ok(new { message = "Rotation updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rotation for player {PlayerId}", playerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error updating player rotation");
        }
    }

    // NPC Management endpoints
    
    [HttpPost("npcs")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NPCDto>> AddNPC([FromBody] AddNPCDto addNPCDto)
    {
        try
        {
            var npc = await _worldSimulationUseCase.AddNPCToWorldAsync(
                addNPCDto.Name,
                addNPCDto.Position,
                addNPCDto.Type,
                addNPCDto.MaxHealth, // Assuming this is the correct property
                addNPCDto.MaxHealth,
                addNPCDto.IsInteractable,
                addNPCDto.Dialogue);

            if (npc == null)
            {
                return BadRequest("Failed to add NPC to world.");
            }

            var npcDto = new NPCDto
            {
                NPCId = npc.Id,
                Name = npc.Name,
                Type = npc.Type,
                Health = npc.Health,
                MaxHealth = npc.MaxHealth,
                IsInteractable = npc.IsInteractable,
                Dialogue = npc.Dialogue,
                Position = npc.Position,
                IsActive = npc.IsActive
            };

            return CreatedAtAction(nameof(GetNPCById), new { npcId = npc.Id }, npcDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding NPC to world");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error adding NPC to world");
        }
    }

    [HttpGet("npcs/{npcId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NPCDto>> GetNPCById(long npcId)
    {
        try
        {
            var npc = await _worldSimulationUseCase.GetNPCByIdAsync(npcId);
            if (npc == null)
            {
                return NotFound($"NPC with ID {npcId} not found");
            }

            var npcDto = new NPCDto
            {
                NPCId = npc.Id,
                Name = npc.Name,
                Type = npc.Type,
                Health = npc.Health,
                MaxHealth = npc.MaxHealth,
                IsInteractable = npc.IsInteractable,
                Dialogue = npc.Dialogue,
                Position = npc.Position,
                IsActive = npc.IsActive
            };

            return Ok(npcDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving NPC with ID {NpcId}", npcId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving NPC");
        }
    }

    // Interaction endpoint
    
    [HttpPost("interactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ProcessInteraction([FromBody] InteractionDto interactionDto)
    {
        try
        {
            var success = await _worldSimulationUseCase.ProcessInteractionAsync(
                interactionDto.InitiatorId,
                interactionDto.TargetId,
                interactionDto.InteractionType);

            if (!success)
            {
                return NotFound("One or both entities not found, or interaction failed");
            }

            return Ok(new { message = "Interaction processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing interaction between {InitiatorId} and {TargetId}",
                interactionDto.InitiatorId, interactionDto.TargetId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error processing interaction");
        }
    }
}

// DTOs for request/response objects

public class CreateWorldDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class WorldDto
{
    public long WorldId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRegionDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Width { get; set; }
    public float Height { get; set; }
    public Vector2 WorldPosition { get; set; }
}

public class RegionDto
{
    public long RegionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Width { get; set; }
    public float Height { get; set; }
    public Vector2 WorldPosition { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddPlayerDto
{
    public Guid CharacterId { get; set; }
    public Guid AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Vector3 Position { get; set; }
}

public class PlayerDto
{
    public long PlayerId { get; set; }
    public Guid AccountId { get; set; }
    public Guid CharacterId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public bool IsOnline { get; set; }
    public Vector3 Position { get; set; }
    public bool IsActive { get; set; }
}

public class AddNPCDto
{
    public string Name { get; set; } = string.Empty;
    public Vector3 Position { get; set; }
    public NPCType Type { get; set; }
    public int MaxHealth { get; set; }
    public bool IsInteractable { get; set; }
    public string Dialogue { get; set; } = string.Empty;
}

public class NPCDto
{
    public long NPCId { get; set; }
    public string Name { get; set; } = string.Empty;
    public NPCType Type { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public bool IsInteractable { get; set; }
    public string Dialogue { get; set; } = string.Empty;
    public Vector3 Position { get; set; }
    public bool IsActive { get; set; }
}

public class UpdatePositionDto
{
    public Vector3 Position { get; set; }
}

public class UpdateRotationDto
{
    public Quaternion Rotation { get; set; }
}

public class InteractionDto
{
    public long InitiatorId { get; set; }
    public long TargetId { get; set; }
    public string InteractionType { get; set; } = string.Empty;
}