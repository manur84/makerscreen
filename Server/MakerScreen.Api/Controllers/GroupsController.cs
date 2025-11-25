using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MakerScreen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly IClientGroupService _groupService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IClientGroupService groupService, ILogger<GroupsController> logger)
    {
        _groupService = groupService;
        _logger = logger;
    }

    /// <summary>
    /// Get all client groups
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientGroup>>> GetAll()
    {
        var groups = await _groupService.GetAllGroupsAsync();
        return Ok(groups);
    }

    /// <summary>
    /// Get a specific group by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ClientGroup>> Get(string id)
    {
        var group = await _groupService.GetGroupAsync(id);
        if (group == null)
        {
            return NotFound();
        }
        return Ok(group);
    }

    /// <summary>
    /// Create a new client group
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClientGroup>> Create([FromBody] ClientGroup group)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
        {
            return BadRequest("Group name is required");
        }

        var created = await _groupService.CreateGroupAsync(group);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update an existing group
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ClientGroup>> Update(string id, [FromBody] ClientGroup group)
    {
        if (id != group.Id)
        {
            return BadRequest("ID mismatch");
        }

        var existing = await _groupService.GetGroupAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var updated = await _groupService.UpdateGroupAsync(group);
        return Ok(updated);
    }

    /// <summary>
    /// Delete a group
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _groupService.DeleteGroupAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Add a client to a group
    /// </summary>
    [HttpPost("{id}/clients/{clientId}")]
    public async Task<ActionResult> AddClient(string id, string clientId)
    {
        var group = await _groupService.GetGroupAsync(id);
        if (group == null)
        {
            return NotFound("Group not found");
        }

        await _groupService.AddClientToGroupAsync(id, clientId);
        return Ok(new { message = $"Client {clientId} added to group {group.Name}" });
    }

    /// <summary>
    /// Remove a client from a group
    /// </summary>
    [HttpDelete("{id}/clients/{clientId}")]
    public async Task<ActionResult> RemoveClient(string id, string clientId)
    {
        var group = await _groupService.GetGroupAsync(id);
        if (group == null)
        {
            return NotFound("Group not found");
        }

        await _groupService.RemoveClientFromGroupAsync(id, clientId);
        return Ok(new { message = $"Client {clientId} removed from group {group.Name}" });
    }

    /// <summary>
    /// Get all groups that a client belongs to
    /// </summary>
    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<IEnumerable<ClientGroup>>> GetGroupsForClient(string clientId)
    {
        var groups = await _groupService.GetGroupsForClientAsync(clientId);
        return Ok(groups);
    }

    /// <summary>
    /// Assign a playlist to all clients in a group
    /// </summary>
    [HttpPost("{id}/playlist/{playlistId}")]
    public async Task<ActionResult> AssignPlaylist(string id, string playlistId)
    {
        var group = await _groupService.GetGroupAsync(id);
        if (group == null)
        {
            return NotFound("Group not found");
        }

        await _groupService.AssignPlaylistToGroupAsync(id, playlistId);
        return Ok(new { message = $"Playlist assigned to group {group.Name}" });
    }

    /// <summary>
    /// Push content to all clients in a group
    /// </summary>
    [HttpPost("{id}/content/{contentId}")]
    public async Task<ActionResult> PushContent(string id, string contentId)
    {
        var group = await _groupService.GetGroupAsync(id);
        if (group == null)
        {
            return NotFound("Group not found");
        }

        await _groupService.PushContentToGroupAsync(id, contentId);
        return Ok(new { message = $"Content pushed to group {group.Name}" });
    }
}
