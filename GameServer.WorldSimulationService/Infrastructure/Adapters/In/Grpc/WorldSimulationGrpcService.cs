using System.Numerics;
using GameServer.WorldSimulationService.Application.Ports.In;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace GameServer.WorldSimulationService.Infrastructure.Adapters.In.Grpc
{
    /// <summary>
    /// gRPC service for world simulation operations
    /// </summary>
    public class WorldSimulationGrpcService : WorldSimulation.WorldSimulationBase
    {
        private readonly IWorldSimulationUseCase _worldSimulationUseCase;
        private readonly ILogger<WorldSimulationGrpcService> _logger;

        public WorldSimulationGrpcService(
            IWorldSimulationUseCase worldSimulationUseCase,
            ILogger<WorldSimulationGrpcService> logger)
        {
            _worldSimulationUseCase = worldSimulationUseCase ?? throw new ArgumentNullException(nameof(worldSimulationUseCase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<PlayerResponse> AddPlayerToWorld(AddPlayerRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Adding player {Username} to world", request.Username);

            try
            {
                var characterId = Guid.Parse(request.CharacterId);
                var accountId = Guid.Parse(request.Accountid);
                var position = new Vector3(request.Position.X, request.Position.Y, request.Position.Z);

                var player = await _worldSimulationUseCase.AddPlayerToWorldAsync(
                    characterId,
                    accountId,
                    request.Username,
                    request.Name,
                    position);

                if (player == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Player not found"));
                }

                return new PlayerResponse
                {
                    PlayerId = player.Id,
                    CharacterId = player.CharacterId.ToString(),
                    Username = player.Username,
                    Name = player.Name,
                    Health = player.Health,
                    MaxHealth = player.MaxHealth,
                    IsOnline = player.IsOnline,
                    Position = new Position
                    {
                        X = player.Position.X,
                        Y = player.Position.Y,
                        Z = player.Position.Z
                    },
                    IsActive = player.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding player to world");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to add player to world"));
            }
        }

        public override async Task<StatusResponse> UpdatePlayerPosition(UpdatePositionRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Updating position for player {PlayerId}", request.EntityId);

            try
            {
                var playerId = request.EntityId;
                var position = new Vector3(request.Position.X, request.Position.Y, request.Position.Z);

                var success = await _worldSimulationUseCase.UpdatePlayerPositionAsync(playerId, position);

                return new StatusResponse
                {
                    Success = success,
                    Message = success ? "Position updated successfully" : "Player not found or invalid position"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating player position");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to update player position"));
            }
        }

        public override async Task<StatusResponse> UpdatePlayerRotation(UpdateRotationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Updating rotation for player {PlayerId}", request.EntityId);

            try
            {
                var playerId = request.EntityId;
                var rotation = new Quaternion(
                    request.Rotation.X,
                    request.Rotation.Y,
                    request.Rotation.Z,
                    request.Rotation.W);

                var success = await _worldSimulationUseCase.UpdatePlayerRotationAsync(playerId, rotation);

                return new StatusResponse
                {
                    Success = success,
                    Message = success ? "Rotation updated successfully" : "Player not found or invalid rotation"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating player rotation");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to update player rotation"));
            }
        }

        public override async Task<NPCResponse> AddNPC(AddNPCRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Adding NPC {Name} to world", request.Name);

            try
            {
                var position = new Vector3(request.Position.X, request.Position.Y, request.Position.Z);

                // Convert the NPCType from the proto enum to our domain enum
                var npcType = (WorldService.Domain.Entities.NPCType)request.Type;

                var npc = await _worldSimulationUseCase.AddNPCToWorldAsync(
                    request.Name,
                    position,
                    npcType,
                    request.MaxHealth,
                    request.IsInteractable,
                    request.Dialogue);

                if (npc == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "NPC could not be created"));
                }

                return new NPCResponse
                {
                    NpcId = npc.Id,
                    Name = npc.Name,
                    Type = (NPCType)npc.Type,
                    Health = npc.Health,
                    MaxHealth = npc.MaxHealth,
                    IsInteractable = npc.IsInteractable,
                    Dialogue = npc.Dialogue,
                    Position = new Position
                    {
                        X = npc.Position.X,
                        Y = npc.Position.Y,
                        Z = npc.Position.Z
                    },
                    IsActive = npc.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding NPC to world");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to add NPC to world"));
            }
        }

        public override async Task<StatusResponse> ProcessInteraction(InteractionRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Processing interaction between {InitiatorId} and {TargetId}", 
                request.InitiatorId, request.TargetId);

            try
            {
                var success = await _worldSimulationUseCase.ProcessInteractionAsync(
                    request.InitiatorId,
                    request.TargetId,
                    request.InteractionType);

                return new StatusResponse
                {
                    Success = success,
                    Message = success ? "Interaction processed successfully" : "Failed to process interaction"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing interaction");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to process interaction"));
            }
        }

        public override async Task<WorldResponse> CreateWorld(CreateWorldRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Creating new world: {Name}", request.Name);

            try
            {
                var world = await _worldSimulationUseCase.CreateNewWorldAsync(
                    request.Name,
                    request.Description);

                if (world == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "World could not be created"));
                }

                return new WorldResponse
                {
                    WorldId = world.Id,
                    Name = world.Name,
                    Description = world.Description,
                    IsActive = world.IsActive,
                    CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(world.CreatedAt, DateTimeKind.Utc))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating world");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to create world"));
            }
        }

        public override async Task<RegionResponse> AddRegion(AddRegionRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Adding region {Name} to world {WorldId}", request.Name, request.WorldId);

            try
            {
                var worldId = request.WorldId;
                var worldPosition = new Vector2(request.WorldPosition.X, request.WorldPosition.Y);

                var region = await _worldSimulationUseCase.AddRegionToWorldAsync(
                    worldId,
                    request.Name,
                    request.Description,
                    request.Width,
                    request.Height,
                    worldPosition);

                if (region == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "World not found"));
                }

                return new RegionResponse
                {
                    RegionId = region.Id,
                    Name = region.Name,
                    Description = region.Description,
                    Width = region.Width,
                    Height = region.Height,
                    WorldPosition = new Vector2D
                    {
                        X = region.WorldPosition.X,
                        Y = region.WorldPosition.Y
                    },
                    IsActive = region.IsActive,
                    CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(region.CreatedAt, DateTimeKind.Utc))
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding region to world");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to add region to world"));
            }
        }
    }
}