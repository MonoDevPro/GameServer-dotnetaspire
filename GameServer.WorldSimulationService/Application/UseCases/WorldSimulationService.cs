using System.Numerics;
using GameServer.WorldService.Domain.Entities;
using GameServer.WorldSimulationService.Application.Ports.In;
using GameServer.WorldSimulationService.Application.Ports.Out;
using GameServer.WorldSimulationService.Domain.Entities;
using GameServer.WorldSimulationService.Domain.Services;

namespace GameServer.WorldSimulationService.Application.UseCases;

/// <summary>
/// Implementation of the world simulation use case
/// </summary>
public class WorldSimulationService : IWorldSimulationUseCase
{
    private readonly IWorldRepository _worldRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly WorldSimulationDomainService _worldSimulationDomainService;

    public WorldSimulationService(
        IWorldRepository worldRepository,
        IAuthenticationService authenticationService,
        WorldSimulationDomainService worldSimulationDomainService)
    {
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _worldSimulationDomainService = worldSimulationDomainService ?? throw new ArgumentNullException(nameof(worldSimulationDomainService));
    }

    public async Task<Player?> AddPlayerToWorldAsync(Guid characterId, Guid accountId, string username, string name, Vector3 position)
    {
        // Default values for a new player
        Quaternion defaultRotation = Quaternion.Identity;
        Vector3 defaultScale = new Vector3(1, 1, 1);
        int defaultMaxHealth = 100;

        // Create a new player entity
        var player = new Player(
            characterId,
            accountId,
            username,
            name,
            position,
            defaultRotation,
            defaultScale,
            defaultMaxHealth);

        // Persist the player to the repository
        return await _worldRepository.CreatePlayerAsync(player);
    }

    public async Task<bool> RemovePlayerFromWorldAsync(long playerId)
    {
        // Retrieve the player to ensure it exists
        var player = await _worldRepository.GetPlayerByIdAsync(playerId);
        if (player == null)
        {
            return false;
        }

        // Set the player as offline before removal
        player.SetOnlineStatus(false);
        await _worldRepository.UpdatePlayerAsync(player);

        // Delete the player from the world
        return await _worldRepository.DeletePlayerAsync(playerId);
    }

    public async Task<bool> UpdatePlayerPositionAsync(long playerId, Vector3 newPosition)
    {
        // Get the player and world
        var player = await _worldRepository.GetPlayerByIdAsync(playerId);
        if (player == null)
            return false;
        if (player.Region == null)
            return false;
        if (player.Region.World == null)
            return false;

        var world = player.Region.World;

        // Use the domain service to simulate and validate the movement
        bool isValid = _worldSimulationDomainService.SimulateMovement(player, newPosition, world);
        if (!isValid)
            return false;

        // Update the player in the repository
        return await _worldRepository.UpdatePlayerAsync(player);
    }

    public async Task<bool> UpdatePlayerRotationAsync(long playerId, Quaternion newRotation)
    {
        // Get the player
        var player = await _worldRepository.GetPlayerByIdAsync(playerId);
        if (player == null)
        {
            return false;
        }

        // Update the rotation
        player.UpdateRotation(newRotation);

        // Save the changes
        return await _worldRepository.UpdatePlayerAsync(player);
    }

    public async Task<NPC?> AddNPCToWorldAsync(string name, Vector3 position, NPCType type, int maxHealth, bool isInteractable, string dialogue)
    {
        // Default values for a new NPC
        Quaternion defaultRotation = Quaternion.Identity;
        Vector3 defaultScale = new Vector3(1, 1, 1);
        Vector3 movementCenter = position;
        float movementRadius = 5.0f; // Default movement radius

        // Create the NPC entity
        var npc = new NPC(
            name,
            position,
            defaultRotation,
            defaultScale,
            type,
            maxHealth,
            isInteractable,
            dialogue,
            movementCenter,
            movementRadius);

        // Persist the NPC to the repository
        return await _worldRepository.CreateNPCAsync(npc);
    }

    public async Task<bool> UpdateNPCPositionAsync(long npcId, Vector3 newPosition)
    {
        // Get the NPC and world
        var npc = await _worldRepository.GetNPCByIdAsync(npcId);
        if (npc == null)
            return false;
        if (npc.Region == null)
            return false;
        if (npc.Region.World == null)
            return false;
        var world = npc.Region.World;

        // Use the domain service to simulate and validate the movement
        bool isValid = _worldSimulationDomainService.SimulateMovement(npc, newPosition, world);
        if (!isValid)
        {
            return false;
        }

        // Update the NPC in the repository
        return await _worldRepository.UpdateNPCAsync(npc);
    }

    public async Task<NPC?> GetNPCByIdAsync(long npcId)
    {
        return await _worldRepository.GetNPCByIdAsync(npcId);
    }

    public async Task<IEnumerable<Entity>> GetEntitiesInRadiusAsync(Vector3 center, float radius)
    {
        return await _worldRepository.GetEntitiesInRadiusAsync(center, radius);
    }

    public async Task<IEnumerable<Player>> GetPlayersInRegionAsync(long regionId)
    {
        return await _worldRepository.GetPlayersByRegionIdAsync(regionId);
    }

    public async Task<Player?> GetPlayerByIdAsync(long playerId)
    {
        return await _worldRepository.GetPlayerByIdAsync(playerId);
    }

    public async Task<IEnumerable<NPC>> GetNPCsInRegionAsync(long regionId)
    {
        return await _worldRepository.GetNPCsByRegionIdAsync(regionId);
    }

    public async Task<bool> ProcessInteractionAsync(long initiatorId, long targetId, string interactionType)
    {
        // Get both entities involved in the interaction
        GameEntity? initiator = await _worldRepository.GetPlayerByIdAsync(initiatorId);
        if (initiator == null)
        {
            // Try if initiator is an NPC
            initiator = await _worldRepository.GetNPCByIdAsync(initiatorId);
            if (initiator == null)
                return false;
        }

        // Get the target entity
        GameEntity? target = await _worldRepository.GetPlayerByIdAsync(targetId);
        if (target == null)
        {
            // Try if target is an NPC
            target = await _worldRepository.GetNPCByIdAsync(targetId);
            if (target == null)
            {
                return false;
            }
        }

        // Use the domain service to process the interaction
        _worldSimulationDomainService.ProcessInteraction(initiator, target, interactionType);

        // Update both entities after the interaction
        bool initiatorUpdated = false;
        bool targetUpdated = false;

        if (initiator is Player playerInitiator)
        {
            initiatorUpdated = await _worldRepository.UpdatePlayerAsync(playerInitiator);
        }
        else if (initiator is NPC npcInitiator)
        {
            initiatorUpdated = await _worldRepository.UpdateNPCAsync(npcInitiator);
        }

        if (target is Player playerTarget)
        {
            targetUpdated = await _worldRepository.UpdatePlayerAsync(playerTarget);
        }
        else if (target is NPC npcTarget)
        {
            targetUpdated = await _worldRepository.UpdateNPCAsync(npcTarget);
        }

        return initiatorUpdated && targetUpdated;
    }

    public async Task<World?> CreateNewWorldAsync(string name, string description)
    {
        var world = new World(name, description);
        return await _worldRepository.CreateWorldAsync(world);
    }

    public async Task<World?> GetWorldByIdAsync(long worldId)
    {
        return await _worldRepository.GetWorldByIdAsync(worldId);
    }

    public async Task<Region?> AddRegionToWorldAsync(long worldId, string name, string description, float width, float height, Vector2 worldPosition)
    {
        // Get the world to ensure it exists
        var world = await _worldRepository.GetWorldByIdAsync(worldId);
        if (world == null)
            return null;

        // Create the new region
        var region = new Region(name, description, width, height, worldPosition);

        // Add it to the world
        world.AddRegion(region);

        // Update the world with the new region
        await _worldRepository.UpdateWorldAsync(world);

        // Save the region to the repository
        return await _worldRepository.CreateRegionAsync(region);
    }
    
    public async Task<Region?> GetRegionByIdAsync(long regionId)
    {
        return await _worldRepository.GetRegionByIdAsync(regionId);
    }
}