using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MakerScreen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmergencyController : ControllerBase
{
    private readonly IEmergencyBroadcastService _emergencyService;
    private readonly ILogger<EmergencyController> _logger;

    public EmergencyController(IEmergencyBroadcastService emergencyService, ILogger<EmergencyController> logger)
    {
        _emergencyService = emergencyService;
        _logger = logger;
    }

    /// <summary>
    /// Get all emergency broadcasts
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmergencyBroadcast>>> GetAll()
    {
        var broadcasts = await _emergencyService.GetAllBroadcastsAsync();
        return Ok(broadcasts);
    }

    /// <summary>
    /// Get active emergency broadcasts
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<EmergencyBroadcast>>> GetActive()
    {
        var broadcasts = await _emergencyService.GetActiveBroadcastsAsync();
        return Ok(broadcasts);
    }

    /// <summary>
    /// Get a specific emergency broadcast by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmergencyBroadcast>> Get(string id)
    {
        var broadcast = await _emergencyService.GetBroadcastAsync(id);
        if (broadcast == null)
        {
            return NotFound();
        }
        return Ok(broadcast);
    }

    /// <summary>
    /// Create a new emergency broadcast
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EmergencyBroadcast>> Create([FromBody] EmergencyBroadcast broadcast)
    {
        if (string.IsNullOrWhiteSpace(broadcast.Title) || string.IsNullOrWhiteSpace(broadcast.Message))
        {
            return BadRequest("Title and message are required");
        }

        var created = await _emergencyService.CreateBroadcastAsync(broadcast);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update an existing emergency broadcast
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmergencyBroadcast>> Update(string id, [FromBody] EmergencyBroadcast broadcast)
    {
        if (id != broadcast.Id)
        {
            return BadRequest("ID mismatch");
        }

        var existing = await _emergencyService.GetBroadcastAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var updated = await _emergencyService.UpdateBroadcastAsync(broadcast);
        return Ok(updated);
    }

    /// <summary>
    /// Delete an emergency broadcast
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _emergencyService.DeleteBroadcastAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Send an emergency broadcast to all clients
    /// </summary>
    [HttpPost("{id}/send")]
    public async Task<ActionResult> Send(string id)
    {
        var broadcast = await _emergencyService.GetBroadcastAsync(id);
        if (broadcast == null)
        {
            return NotFound();
        }

        await _emergencyService.SendBroadcastAsync(id);
        _logger.LogWarning("Emergency broadcast sent: {Title}", broadcast.Title);
        return Ok(new { message = "Emergency broadcast sent to all clients" });
    }

    /// <summary>
    /// Send an emergency broadcast to a specific group
    /// </summary>
    [HttpPost("{id}/send/group/{groupId}")]
    public async Task<ActionResult> SendToGroup(string id, string groupId)
    {
        var broadcast = await _emergencyService.GetBroadcastAsync(id);
        if (broadcast == null)
        {
            return NotFound("Broadcast not found");
        }

        await _emergencyService.SendBroadcastToGroupAsync(id, groupId);
        _logger.LogWarning("Emergency broadcast sent to group {GroupId}: {Title}", groupId, broadcast.Title);
        return Ok(new { message = $"Emergency broadcast sent to group {groupId}" });
    }

    /// <summary>
    /// Create and immediately send an emergency broadcast
    /// </summary>
    [HttpPost("send-immediate")]
    public async Task<ActionResult<EmergencyBroadcast>> SendImmediate([FromBody] EmergencyBroadcast broadcast)
    {
        if (string.IsNullOrWhiteSpace(broadcast.Title) || string.IsNullOrWhiteSpace(broadcast.Message))
        {
            return BadRequest("Title and message are required");
        }

        var created = await _emergencyService.CreateBroadcastAsync(broadcast);
        await _emergencyService.SendBroadcastAsync(created.Id);
        
        _logger.LogWarning("Immediate emergency broadcast created and sent: {Title}", broadcast.Title);
        return Ok(created);
    }

    /// <summary>
    /// Clear a specific emergency broadcast
    /// </summary>
    [HttpPost("{id}/clear")]
    public async Task<ActionResult> Clear(string id)
    {
        var broadcast = await _emergencyService.GetBroadcastAsync(id);
        if (broadcast == null)
        {
            return NotFound();
        }

        await _emergencyService.ClearBroadcastAsync(id);
        return Ok(new { message = "Emergency broadcast cleared" });
    }

    /// <summary>
    /// Clear all active emergency broadcasts
    /// </summary>
    [HttpPost("clear-all")]
    public async Task<ActionResult> ClearAll()
    {
        await _emergencyService.ClearAllBroadcastsAsync();
        _logger.LogInformation("All emergency broadcasts cleared");
        return Ok(new { message = "All emergency broadcasts cleared" });
    }
}
