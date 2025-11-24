using System.Collections.Concurrent;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.Content;

/// <summary>
/// Manages digital signage content
/// </summary>
public class ContentService : IContentService
{
    private readonly ILogger<ContentService> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly ConcurrentDictionary<string, ContentItem> _contentStore = new();
    private readonly string _contentPath;

    public ContentService(
        ILogger<ContentService> logger,
        IWebSocketServer webSocketServer)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _contentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
        Directory.CreateDirectory(_contentPath);
    }

    public async Task<ContentItem> AddContentAsync(ContentItem content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding content: {Name}", content.Name);

        try
        {
            // Save content data to disk
            var filePath = Path.Combine(_contentPath, $"{content.Id}{GetFileExtension(content.MimeType)}");
            await File.WriteAllBytesAsync(filePath, content.Data, cancellationToken);

            // Store metadata
            _contentStore.TryAdd(content.Id, content);

            _logger.LogInformation("Content added successfully: {Id}", content.Id);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding content");
            throw;
        }
    }

    public async Task<ContentItem?> GetContentAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_contentStore.TryGetValue(id, out var content))
        {
            // Load data from disk if needed
            if (content.Data.Length == 0)
            {
                var filePath = Path.Combine(_contentPath, $"{content.Id}{GetFileExtension(content.MimeType)}");
                if (File.Exists(filePath))
                {
                    content.Data = await File.ReadAllBytesAsync(filePath, cancellationToken);
                }
            }

            return content;
        }

        return null;
    }

    public Task<IEnumerable<ContentItem>> GetAllContentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ContentItem>>(_contentStore.Values.ToList());
    }

    public async Task<bool> DeleteContentAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting content: {Id}", id);

        try
        {
            if (_contentStore.TryRemove(id, out var content))
            {
                var filePath = Path.Combine(_contentPath, $"{content.Id}{GetFileExtension(content.MimeType)}");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                _logger.LogInformation("Content deleted successfully: {Id}", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting content {Id}", id);
            return false;
        }
    }

    public async Task PushContentToClientsAsync(string contentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pushing content {ContentId} to all clients", contentId);

        try
        {
            var content = await GetContentAsync(contentId, cancellationToken);
            if (content == null)
            {
                _logger.LogWarning("Content {ContentId} not found", contentId);
                return;
            }

            var message = new WebSocketMessage
            {
                Type = MessageTypes.ContentUpdate,
                Data = new
                {
                    contentId = content.Id,
                    name = content.Name,
                    type = content.Type.ToString(),
                    mimeType = content.MimeType,
                    duration = content.Duration,
                    data = Convert.ToBase64String(content.Data)
                }
            };

            await _webSocketServer.BroadcastMessageAsync(message, cancellationToken);

            _logger.LogInformation("Content pushed to clients successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing content to clients");
            throw;
        }
    }

    private string GetFileExtension(string mimeType)
    {
        return mimeType.ToLower() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "video/mp4" => ".mp4",
            "text/html" => ".html",
            _ => ".bin"
        };
    }
}
