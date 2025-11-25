using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Overlay = MakerScreen.Core.Models.Overlay;

namespace MakerScreen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OverlaysController : ControllerBase
{
    private readonly IOverlayService _overlayService;
    private readonly ILogger<OverlaysController> _logger;

    public OverlaysController(IOverlayService overlayService, ILogger<OverlaysController> logger)
    {
        _overlayService = overlayService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Overlay>>> GetAll()
    {
        var overlays = await _overlayService.GetAllOverlaysAsync();
        return Ok(overlays);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Overlay>> Get(string id)
    {
        var overlay = await _overlayService.GetOverlayAsync(id);
        if (overlay == null)
        {
            return NotFound();
        }
        return Ok(overlay);
    }

    [HttpPost]
    public async Task<ActionResult<Overlay>> Create([FromBody] Overlay overlay)
    {
        var created = await _overlayService.CreateOverlayAsync(overlay);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Overlay>> Update(string id, [FromBody] Overlay overlay)
    {
        if (id != overlay.Id)
        {
            return BadRequest("ID mismatch");
        }

        var existing = await _overlayService.GetOverlayAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var updated = await _overlayService.UpdateOverlayAsync(overlay);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _overlayService.DeleteOverlayAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("{id}/render")]
    public async Task<ActionResult<string>> Render(string id)
    {
        var content = await _overlayService.RenderOverlayAsync(id);
        if (string.IsNullOrEmpty(content))
        {
            return NotFound();
        }
        return Content(content, "text/html");
    }

    [HttpPost("{id}/assign")]
    public async Task<ActionResult> AssignToClients(string id, [FromBody] AssignOverlayRequest request)
    {
        await _overlayService.AssignOverlayToClientsAsync(id, request.ClientIds);
        return Ok(new { message = "Overlay assigned to clients" });
    }
}

public class AssignOverlayRequest
{
    public List<string> ClientIds { get; set; } = new();
}
