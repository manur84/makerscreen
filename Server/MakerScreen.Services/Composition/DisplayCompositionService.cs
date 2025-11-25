using System.Collections.Concurrent;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.Composition;

/// <summary>
/// Service for managing display compositions (scenes with background and overlays)
/// </summary>
public class DisplayCompositionService : IDisplayCompositionService
{
    private readonly ILogger<DisplayCompositionService> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly IOverlayService _overlayService;
    private readonly IClientGroupService? _clientGroupService;
    private readonly ConcurrentDictionary<string, DisplayComposition> _compositions = new();

    public DisplayCompositionService(
        ILogger<DisplayCompositionService> logger,
        IWebSocketServer webSocketServer,
        IOverlayService overlayService,
        IClientGroupService? clientGroupService = null)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _overlayService = overlayService;
        _clientGroupService = clientGroupService;
    }

    public Task<DisplayComposition> CreateCompositionAsync(DisplayComposition composition, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating composition: {Name}", composition.Name);
        
        composition.CreatedAt = DateTime.UtcNow;
        composition.ModifiedAt = DateTime.UtcNow;
        
        _compositions.TryAdd(composition.Id, composition);
        
        return Task.FromResult(composition);
    }

    public Task<DisplayComposition?> GetCompositionAsync(string id, CancellationToken cancellationToken = default)
    {
        _compositions.TryGetValue(id, out var composition);
        return Task.FromResult(composition);
    }

    public Task<IEnumerable<DisplayComposition>> GetAllCompositionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<DisplayComposition>>(_compositions.Values.ToList());
    }

    public Task<DisplayComposition> UpdateCompositionAsync(DisplayComposition composition, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating composition: {Id}", composition.Id);
        
        composition.ModifiedAt = DateTime.UtcNow;
        _compositions[composition.Id] = composition;
        
        return Task.FromResult(composition);
    }

    public Task<bool> DeleteCompositionAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting composition: {Id}", id);
        return Task.FromResult(_compositions.TryRemove(id, out _));
    }

    public async Task<DisplayComposition> AddOverlayToCompositionAsync(string compositionId, CompositionOverlay overlay, CancellationToken cancellationToken = default)
    {
        var composition = await GetCompositionAsync(compositionId, cancellationToken);
        if (composition == null)
        {
            throw new InvalidOperationException($"Composition {compositionId} not found");
        }

        // Set Z-Index to be on top
        overlay.ZIndex = composition.Overlays.Count > 0 
            ? composition.Overlays.Max(o => o.ZIndex) + 1 
            : 0;
        
        composition.Overlays.Add(overlay);
        composition.ModifiedAt = DateTime.UtcNow;
        
        _logger.LogInformation("Added overlay {OverlayId} to composition {CompositionId}", overlay.OverlayId, compositionId);
        
        return composition;
    }

    public async Task<DisplayComposition> RemoveOverlayFromCompositionAsync(string compositionId, string overlayPlacementId, CancellationToken cancellationToken = default)
    {
        var composition = await GetCompositionAsync(compositionId, cancellationToken);
        if (composition == null)
        {
            throw new InvalidOperationException($"Composition {compositionId} not found");
        }

        var overlayToRemove = composition.Overlays.FirstOrDefault(o => o.Id == overlayPlacementId);
        if (overlayToRemove != null)
        {
            composition.Overlays.Remove(overlayToRemove);
            composition.ModifiedAt = DateTime.UtcNow;
            _logger.LogInformation("Removed overlay placement {OverlayPlacementId} from composition {CompositionId}", overlayPlacementId, compositionId);
        }
        
        return composition;
    }

    public async Task<DisplayComposition> UpdateOverlayInCompositionAsync(string compositionId, CompositionOverlay overlay, CancellationToken cancellationToken = default)
    {
        var composition = await GetCompositionAsync(compositionId, cancellationToken);
        if (composition == null)
        {
            throw new InvalidOperationException($"Composition {compositionId} not found");
        }

        var existingOverlay = composition.Overlays.FirstOrDefault(o => o.Id == overlay.Id);
        if (existingOverlay != null)
        {
            var index = composition.Overlays.IndexOf(existingOverlay);
            composition.Overlays[index] = overlay;
            composition.ModifiedAt = DateTime.UtcNow;
            _logger.LogInformation("Updated overlay {OverlayId} in composition {CompositionId}", overlay.Id, compositionId);
        }
        
        return composition;
    }

    public async Task<DisplayComposition> SetBackgroundAsync(string compositionId, CompositionBackground background, CancellationToken cancellationToken = default)
    {
        var composition = await GetCompositionAsync(compositionId, cancellationToken);
        if (composition == null)
        {
            throw new InvalidOperationException($"Composition {compositionId} not found");
        }

        composition.Background = background;
        composition.ModifiedAt = DateTime.UtcNow;
        
        _logger.LogInformation("Set background for composition {CompositionId}: Type={Type}", compositionId, background.Type);
        
        return composition;
    }

    public async Task<DisplayComposition> SetResolutionAsync(string compositionId, CompositionResolution resolution, CancellationToken cancellationToken = default)
    {
        var composition = await GetCompositionAsync(compositionId, cancellationToken);
        if (composition == null)
        {
            throw new InvalidOperationException($"Composition {compositionId} not found");
        }

        composition.Resolution = resolution;
        composition.ModifiedAt = DateTime.UtcNow;
        
        _logger.LogInformation("Set resolution for composition {CompositionId}: {Width}x{Height}", compositionId, resolution.Width, resolution.Height);
        
        return composition;
    }

    public async Task PublishCompositionAsync(string compositionId, CancellationToken cancellationToken = default)
    {
        var composition = await GetCompositionAsync(compositionId, cancellationToken);
        if (composition == null)
        {
            _logger.LogWarning("Composition {CompositionId} not found for publishing", compositionId);
            return;
        }

        _logger.LogInformation("Publishing composition {CompositionId} to all clients", compositionId);
        
        var message = await CreateCompositionMessageAsync(composition, cancellationToken);
        await _webSocketServer.BroadcastMessageAsync(message, cancellationToken);
    }

    public async Task PublishCompositionToClientsAsync(string compositionId, IEnumerable<string> clientIds, CancellationToken cancellationToken = default)
    {
        var composition = await GetCompositionAsync(compositionId, cancellationToken);
        if (composition == null)
        {
            _logger.LogWarning("Composition {CompositionId} not found for publishing", compositionId);
            return;
        }

        _logger.LogInformation("Publishing composition {CompositionId} to specific clients", compositionId);
        
        var message = await CreateCompositionMessageAsync(composition, cancellationToken);
        
        foreach (var clientId in clientIds)
        {
            message.ClientId = clientId;
            await _webSocketServer.SendMessageAsync(clientId, message, cancellationToken);
        }
    }

    public async Task PublishCompositionToGroupAsync(string compositionId, string groupId, CancellationToken cancellationToken = default)
    {
        if (_clientGroupService == null)
        {
            _logger.LogWarning("ClientGroupService not available");
            return;
        }

        var group = await _clientGroupService.GetGroupAsync(groupId, cancellationToken);
        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found", groupId);
            return;
        }

        _logger.LogInformation("Publishing composition {CompositionId} to group {GroupId}", compositionId, groupId);
        
        await PublishCompositionToClientsAsync(compositionId, group.ClientIds, cancellationToken);
    }

    public async Task<string> RenderCompositionPreviewAsync(string compositionId, CancellationToken cancellationToken = default)
    {
        var composition = await GetCompositionAsync(compositionId, cancellationToken);
        if (composition == null)
        {
            return string.Empty;
        }

        // Generate HTML preview of the composition
        var html = GenerateCompositionHtml(composition);
        return html;
    }

    private async Task<WebSocketMessage> CreateCompositionMessageAsync(DisplayComposition composition, CancellationToken cancellationToken)
    {
        // Render all overlays
        var renderedOverlays = new List<object>();
        foreach (var overlayPlacement in composition.Overlays.Where(o => o.IsVisible))
        {
            var overlay = await _overlayService.GetOverlayAsync(overlayPlacement.OverlayId, cancellationToken);
            if (overlay != null)
            {
                var renderedContent = await _overlayService.RenderOverlayAsync(overlayPlacement.OverlayId, cancellationToken);
                renderedOverlays.Add(new
                {
                    id = overlayPlacement.Id,
                    overlayId = overlayPlacement.OverlayId,
                    name = overlayPlacement.Name,
                    x = overlayPlacement.X,
                    y = overlayPlacement.Y,
                    width = overlayPlacement.Width,
                    height = overlayPlacement.Height,
                    zIndex = overlayPlacement.ZIndex,
                    content = renderedContent,
                    overlay = new
                    {
                        type = overlay.Type.ToString(),
                        style = overlay.Style
                    }
                });
            }
        }

        return new WebSocketMessage
        {
            Type = MessageTypes.Command,
            Data = new
            {
                command = "composition_update",
                composition = new
                {
                    id = composition.Id,
                    name = composition.Name,
                    resolution = new
                    {
                        width = composition.Resolution.Width,
                        height = composition.Resolution.Height
                    },
                    background = new
                    {
                        type = composition.Background.Type.ToString(),
                        color = composition.Background.Color,
                        imageContentId = composition.Background.ImageContentId,
                        imageData = composition.Background.ImageData != null 
                            ? Convert.ToBase64String(composition.Background.ImageData) 
                            : null,
                        scaleMode = composition.Background.ScaleMode.ToString()
                    },
                    overlays = renderedOverlays
                }
            }
        };
    }

    private string GenerateCompositionHtml(DisplayComposition composition)
    {
        var overlaysHtml = string.Join("\n", composition.Overlays
            .Where(o => o.IsVisible)
            .OrderBy(o => o.ZIndex)
            .Select(o => $@"
                <div style='position:absolute; left:{o.X}px; top:{o.Y}px; width:{o.Width}px; height:{o.Height}px; z-index:{o.ZIndex};'>
                    <!-- Overlay: {o.Name} -->
                </div>"));

        var backgroundStyle = composition.Background.Type switch
        {
            BackgroundType.Color => $"background-color:{composition.Background.Color};",
            BackgroundType.Image => $"background-image:url('data:image/png;base64,{(composition.Background.ImageData != null ? Convert.ToBase64String(composition.Background.ImageData) : "")}'); background-size:{MapScaleModeToCss(composition.Background.ScaleMode)}; background-repeat:no-repeat; background-position:center;",
            _ => ""
        };

        return $@"
            <div style='position:relative; width:{composition.Resolution.Width}px; height:{composition.Resolution.Height}px; {backgroundStyle}'>
                {overlaysHtml}
            </div>";
    }

    private static string MapScaleModeToCss(ImageScaleMode scaleMode)
    {
        return scaleMode switch
        {
            ImageScaleMode.Fill => "cover",
            ImageScaleMode.Fit => "contain",
            ImageScaleMode.Stretch => "100% 100%",
            ImageScaleMode.Center => "auto",
            ImageScaleMode.Tile => "auto",
            _ => "cover"
        };
    }
}
