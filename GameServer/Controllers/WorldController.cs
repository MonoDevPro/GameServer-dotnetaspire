using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using GameServer.WorldService.Application.Ports;
using GameServer.WorldService.Domain.Entities;

namespace GameServer.WorldService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorldController : ControllerBase
    {
        private readonly IWorldService _worldService;

        public WorldController(IWorldService worldService)
        {
            _worldService = worldService;
        }

        [HttpGet("worlds")]
        public async Task<ActionResult<IEnumerable<World>>> GetWorlds()
        {
            var worlds = await _worldService.GetAllWorldsAsync();
            return Ok(worlds);
        }

        [HttpGet("worlds/{id}")]
        public async Task<ActionResult<World>> GetWorldById(Guid id)
        {
            var world = await _worldService.GetWorldAsync(id);
            if (world == null)
                return NotFound();

            return Ok(world);
        }

        [HttpPost("worlds")]
        public async Task<ActionResult<World>> CreateWorld([FromBody] CreateWorldRequest request)
        {
            var world = await _worldService.CreateWorldAsync(request.Name, request.Description);
            return CreatedAtAction(nameof(GetWorldById), new { id = world.Id }, world);
        }

        [HttpGet("regions/{id}")]
        public async Task<ActionResult<Region>> GetRegionById(Guid id)
        {
            var region = await _worldService.GetRegionAsync(id);
            if (region == null)
                return NotFound();

            return Ok(region);
        }

        [HttpGet("worlds/{worldId}/regions")]
        public async Task<ActionResult<IEnumerable<Region>>> GetRegionsByWorldId(Guid worldId)
        {
            var regions = await _worldService.GetRegionsByWorldAsync(worldId);
            return Ok(regions);
        }

        [HttpPost("worlds/{worldId}/regions")]
        public async Task<ActionResult<Region>> CreateRegion(Guid worldId, [FromBody] CreateRegionRequest request)
        {
            var region = await _worldService.CreateRegionAsync(
                request.Name,
                request.Description,
                request.Width,
                request.Height,
                new Vector2(request.PositionX, request.PositionY),
                worldId);

            return CreatedAtAction(nameof(GetRegionById), new { id = region.Id }, region);
        }
    }

    public class CreateWorldRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CreateRegionRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
    }
}