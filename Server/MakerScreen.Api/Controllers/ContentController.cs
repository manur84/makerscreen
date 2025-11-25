using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MakerScreen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly IContentService _contentService;
    private readonly ILogger<ContentController> _logger;

    public ContentController(IContentService contentService, ILogger<ContentController> logger)
    {
        _contentService = contentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContentItem>>> GetAll()
    {
        var content = await _contentService.GetAllContentAsync();
        return Ok(content);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContentItem>> Get(string id)
    {
        var content = await _contentService.GetContentAsync(id);
        if (content == null)
        {
            return NotFound();
        }
        return Ok(content);
    }

    [HttpPost]
    public async Task<ActionResult<ContentItem>> Create([FromBody] ContentItemRequest request)
    {
        var content = new ContentItem
        {
            Name = request.Name,
            Type = request.Type,
            MimeType = request.MimeType,
            Duration = request.Duration,
            Data = Convert.FromBase64String(request.DataBase64 ?? string.Empty)
        };

        var created = await _contentService.AddContentAsync(content);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ContentItem>> Upload(IFormFile file, [FromForm] string? name = null, [FromForm] int duration = 10)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var content = new ContentItem
        {
            Name = name ?? file.FileName,
            Type = DetermineContentType(file.ContentType),
            MimeType = file.ContentType,
            Duration = duration,
            Data = ms.ToArray()
        };

        var created = await _contentService.AddContentAsync(content);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _contentService.DeleteContentAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("{id}/push")]
    public async Task<ActionResult> PushToClients(string id)
    {
        await _contentService.PushContentToClientsAsync(id);
        return Ok(new { message = "Content pushed to all clients" });
    }

    private ContentType DetermineContentType(string mimeType)
    {
        return mimeType.ToLower() switch
        {
            "image/png" or "image/jpeg" or "image/gif" => ContentType.Image,
            "video/mp4" or "video/webm" => ContentType.Video,
            "text/html" => ContentType.Html,
            _ => ContentType.Image
        };
    }
}

public class ContentItemRequest
{
    public string Name { get; set; } = string.Empty;
    public ContentType Type { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public int Duration { get; set; } = 10;
    public string? DataBase64 { get; set; }
}
