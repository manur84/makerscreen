using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MakerScreen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaylistsController : ControllerBase
{
    private readonly IPlaylistService _playlistService;
    private readonly ILogger<PlaylistsController> _logger;

    public PlaylistsController(IPlaylistService playlistService, ILogger<PlaylistsController> logger)
    {
        _playlistService = playlistService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Playlist>>> GetAll()
    {
        var playlists = await _playlistService.GetAllPlaylistsAsync();
        return Ok(playlists);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Playlist>> Get(string id)
    {
        var playlist = await _playlistService.GetPlaylistAsync(id);
        if (playlist == null)
        {
            return NotFound();
        }
        return Ok(playlist);
    }

    [HttpPost]
    public async Task<ActionResult<Playlist>> Create([FromBody] Playlist playlist)
    {
        var created = await _playlistService.CreatePlaylistAsync(playlist);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Playlist>> Update(string id, [FromBody] Playlist playlist)
    {
        if (id != playlist.Id)
        {
            return BadRequest("ID mismatch");
        }

        var existing = await _playlistService.GetPlaylistAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var updated = await _playlistService.UpdatePlaylistAsync(playlist);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _playlistService.DeletePlaylistAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("{id}/assign")]
    public async Task<ActionResult> AssignToClients(string id, [FromBody] AssignClientsRequest request)
    {
        await _playlistService.AssignPlaylistToClientsAsync(id, request.ClientIds);
        return Ok(new { message = "Playlist assigned to clients" });
    }

    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<Playlist>> GetActivePlaylistForClient(string clientId)
    {
        var playlist = await _playlistService.GetActivePlaylistForClientAsync(clientId);
        if (playlist == null)
        {
            return NotFound();
        }
        return Ok(playlist);
    }
}

public class AssignClientsRequest
{
    public List<string> ClientIds { get; set; } = new();
}
