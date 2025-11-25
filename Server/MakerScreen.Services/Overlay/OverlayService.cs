using System.Collections.Concurrent;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.Overlay;

/// <summary>
/// Manages overlay creation and rendering
/// </summary>
public class OverlayService : IOverlayService
{
    private readonly ILogger<OverlayService> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly ConcurrentDictionary<string, Core.Models.Overlay> _overlays = new();
    private readonly ConcurrentDictionary<string, List<string>> _clientOverlayAssignments = new();

    public OverlayService(
        ILogger<OverlayService> logger,
        IWebSocketServer webSocketServer)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
    }

    public Task<Core.Models.Overlay> CreateOverlayAsync(Core.Models.Overlay overlay, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating overlay: {Name}", overlay.Name);
        
        _overlays.TryAdd(overlay.Id, overlay);
        
        return Task.FromResult(overlay);
    }

    public Task<Core.Models.Overlay?> GetOverlayAsync(string id, CancellationToken cancellationToken = default)
    {
        _overlays.TryGetValue(id, out var overlay);
        return Task.FromResult(overlay);
    }

    public Task<IEnumerable<Core.Models.Overlay>> GetAllOverlaysAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Core.Models.Overlay>>(_overlays.Values.ToList());
    }

    public Task<Core.Models.Overlay> UpdateOverlayAsync(Core.Models.Overlay overlay, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating overlay: {Id}", overlay.Id);
        
        _overlays[overlay.Id] = overlay;
        
        return Task.FromResult(overlay);
    }

    public Task<bool> DeleteOverlayAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting overlay: {Id}", id);
        
        return Task.FromResult(_overlays.TryRemove(id, out _));
    }

    public async Task<string> RenderOverlayAsync(string overlayId, CancellationToken cancellationToken = default)
    {
        var overlay = await GetOverlayAsync(overlayId, cancellationToken);
        if (overlay == null)
        {
            return string.Empty;
        }

        return overlay.Type switch
        {
            OverlayType.Text => RenderTextOverlay(overlay),
            OverlayType.DateTime => RenderDateTimeOverlay(overlay),
            OverlayType.Weather => await RenderWeatherOverlayAsync(overlay, cancellationToken),
            OverlayType.Ticker => RenderTickerOverlay(overlay),
            OverlayType.SqlData => await RenderSqlOverlayAsync(overlay, cancellationToken),
            OverlayType.Html => overlay.Content,
            OverlayType.QrCode => RenderQrCodeOverlay(overlay),
            OverlayType.Logo => RenderLogoOverlay(overlay),
            _ => string.Empty
        };
    }

    public async Task AssignOverlayToClientsAsync(string overlayId, IEnumerable<string> clientIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assigning overlay {OverlayId} to clients", overlayId);
        
        var overlay = await GetOverlayAsync(overlayId, cancellationToken);
        if (overlay == null)
        {
            _logger.LogWarning("Overlay {OverlayId} not found", overlayId);
            return;
        }

        foreach (var clientId in clientIds)
        {
            if (!_clientOverlayAssignments.ContainsKey(clientId))
            {
                _clientOverlayAssignments[clientId] = new List<string>();
            }
            
            if (!_clientOverlayAssignments[clientId].Contains(overlayId))
            {
                _clientOverlayAssignments[clientId].Add(overlayId);
            }

            var renderedContent = await RenderOverlayAsync(overlayId, cancellationToken);
            
            var message = new WebSocketMessage
            {
                Type = MessageTypes.Command,
                ClientId = clientId,
                Data = new
                {
                    command = "overlay_update",
                    overlayId,
                    overlay = SerializeOverlay(overlay),
                    content = renderedContent
                }
            };
            
            await _webSocketServer.SendMessageAsync(clientId, message, cancellationToken);
        }
    }

    private string RenderTextOverlay(Core.Models.Overlay overlay)
    {
        return $@"<div style='{GenerateStyle(overlay.Style)}'>{overlay.Content}</div>";
    }

    private string RenderDateTimeOverlay(Core.Models.Overlay overlay)
    {
        var format = overlay.Content ?? "yyyy-MM-dd HH:mm:ss";
        var dateTime = DateTime.Now.ToString(format);
        return $@"<div style='{GenerateStyle(overlay.Style)}'>{dateTime}</div>";
    }

    private async Task<string> RenderWeatherOverlayAsync(Core.Models.Overlay overlay, CancellationToken cancellationToken)
    {
        // Weather API integration would go here
        return $@"<div style='{GenerateStyle(overlay.Style)}'>Weather: 22°C</div>";
    }

    private string RenderTickerOverlay(Core.Models.Overlay overlay)
    {
        // Use CSS animation instead of deprecated marquee tag
        var style = GenerateStyle(overlay.Style);
        return $@"<div style='{style}overflow:hidden;'>
            <div style='display:inline-block;animation:scroll 10s linear infinite;white-space:nowrap;'>
                {overlay.Content}
            </div>
            <style>@keyframes scroll {{ 0% {{ transform:translateX(100%); }} 100% {{ transform:translateX(-100%); }} }}</style>
        </div>";
    }

    private async Task<string> RenderSqlOverlayAsync(Core.Models.Overlay overlay, CancellationToken cancellationToken)
    {
        // SQL query execution would go here
        // For now, return placeholder
        _logger.LogInformation("SQL overlay query: {Query}", overlay.SqlQuery);
        return $@"<div style='{GenerateStyle(overlay.Style)}'>[SQL Data]</div>";
    }

    private string RenderQrCodeOverlay(Core.Models.Overlay overlay)
    {
        // QR code generation would go here
        return $@"<div style='{GenerateStyle(overlay.Style)}'>[QR Code: {overlay.Content}]</div>";
    }

    private string RenderLogoOverlay(Core.Models.Overlay overlay)
    {
        return $@"<img src='{overlay.Content}' style='{GenerateStyle(overlay.Style)}' />";
    }

    private string GenerateStyle(OverlayStyle style)
    {
        return $"font-family:{style.FontFamily};" +
               $"font-size:{style.FontSize}px;" +
               $"color:{style.FontColor};" +
               $"background-color:{style.BackgroundColor};" +
               $"border-radius:{style.BorderRadius}px;" +
               $"padding:{style.Padding}px;";
    }

    private object SerializeOverlay(Core.Models.Overlay overlay)
    {
        return new
        {
            id = overlay.Id,
            name = overlay.Name,
            type = overlay.Type.ToString(),
            position = new
            {
                x = overlay.Position.X,
                y = overlay.Position.Y,
                width = overlay.Position.Width,
                height = overlay.Position.Height,
                anchor = overlay.Position.Anchor.ToString()
            },
            style = new
            {
                fontFamily = overlay.Style.FontFamily,
                fontSize = overlay.Style.FontSize,
                fontColor = overlay.Style.FontColor,
                backgroundColor = overlay.Style.BackgroundColor,
                borderRadius = overlay.Style.BorderRadius,
                padding = overlay.Style.Padding,
                shadow = overlay.Style.Shadow
            },
            refreshInterval = overlay.RefreshInterval
        };
    }
}
