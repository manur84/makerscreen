using System.Collections.Concurrent;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.Emergency;

/// <summary>
/// Manages emergency broadcasts that interrupt normal content
/// </summary>
public class EmergencyBroadcastService : IEmergencyBroadcastService
{
    private readonly ILogger<EmergencyBroadcastService> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly IClientGroupService? _clientGroupService;
    private readonly ConcurrentDictionary<string, EmergencyBroadcast> _broadcasts = new();

    public EmergencyBroadcastService(
        ILogger<EmergencyBroadcastService> logger,
        IWebSocketServer webSocketServer,
        IClientGroupService? clientGroupService = null)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _clientGroupService = clientGroupService;
    }

    public Task<EmergencyBroadcast> CreateBroadcastAsync(EmergencyBroadcast broadcast, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating emergency broadcast: {Title}", broadcast.Title);
        
        _broadcasts.TryAdd(broadcast.Id, broadcast);
        
        return Task.FromResult(broadcast);
    }

    public Task<EmergencyBroadcast?> GetBroadcastAsync(string id, CancellationToken cancellationToken = default)
    {
        _broadcasts.TryGetValue(id, out var broadcast);
        return Task.FromResult(broadcast);
    }

    public Task<IEnumerable<EmergencyBroadcast>> GetAllBroadcastsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<EmergencyBroadcast>>(_broadcasts.Values.ToList());
    }

    public Task<IEnumerable<EmergencyBroadcast>> GetActiveBroadcastsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var active = _broadcasts.Values
            .Where(b => b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > now))
            .OrderByDescending(b => b.Priority)
            .ToList();
        
        return Task.FromResult<IEnumerable<EmergencyBroadcast>>(active);
    }

    public Task<EmergencyBroadcast> UpdateBroadcastAsync(EmergencyBroadcast broadcast, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating emergency broadcast: {Id}", broadcast.Id);
        
        _broadcasts[broadcast.Id] = broadcast;
        
        return Task.FromResult(broadcast);
    }

    public Task<bool> DeleteBroadcastAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting emergency broadcast: {Id}", id);
        
        return Task.FromResult(_broadcasts.TryRemove(id, out _));
    }

    public async Task SendBroadcastAsync(string broadcastId, CancellationToken cancellationToken = default)
    {
        var broadcast = await GetBroadcastAsync(broadcastId, cancellationToken);
        if (broadcast == null)
        {
            _logger.LogWarning("Emergency broadcast {BroadcastId} not found", broadcastId);
            return;
        }

        broadcast.IsActive = true;
        
        var message = CreateBroadcastMessage(broadcast);

        // Send to specific clients if targeted
        var targetClientIds = broadcast.TargetClientIds;
        if (targetClientIds?.Any() == true)
        {
            foreach (var clientId in targetClientIds)
            {
                message.ClientId = clientId;
                await _webSocketServer.SendMessageAsync(clientId, message, cancellationToken);
            }
            
            _logger.LogWarning("EMERGENCY BROADCAST sent to {ClientCount} targeted clients: {Title}", 
                targetClientIds.Count, broadcast.Title);
        }
        else
        {
            // Broadcast to all clients
            await _webSocketServer.BroadcastMessageAsync(message, cancellationToken);
            
            _logger.LogWarning("EMERGENCY BROADCAST sent to ALL clients: {Title}", broadcast.Title);
        }
    }

    public async Task SendBroadcastToGroupAsync(string broadcastId, string groupId, CancellationToken cancellationToken = default)
    {
        if (_clientGroupService == null)
        {
            _logger.LogWarning("Client group service not available");
            return;
        }

        var broadcast = await GetBroadcastAsync(broadcastId, cancellationToken);
        if (broadcast == null)
        {
            _logger.LogWarning("Emergency broadcast {BroadcastId} not found", broadcastId);
            return;
        }

        var group = await _clientGroupService.GetGroupAsync(groupId, cancellationToken);
        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found", groupId);
            return;
        }

        broadcast.IsActive = true;
        broadcast.TargetGroupIds = new List<string> { groupId };
        
        var message = CreateBroadcastMessage(broadcast);

        foreach (var clientId in group.ClientIds)
        {
            message.ClientId = clientId;
            await _webSocketServer.SendMessageAsync(clientId, message, cancellationToken);
        }

        _logger.LogWarning("EMERGENCY BROADCAST sent to group {GroupName} ({ClientCount} clients): {Title}", 
            group.Name, group.ClientIds.Count, broadcast.Title);
    }

    public async Task ClearBroadcastAsync(string broadcastId, CancellationToken cancellationToken = default)
    {
        var broadcast = await GetBroadcastAsync(broadcastId, cancellationToken);
        if (broadcast == null)
        {
            _logger.LogWarning("Emergency broadcast {BroadcastId} not found", broadcastId);
            return;
        }

        broadcast.IsActive = false;
        
        var message = new WebSocketMessage
        {
            Type = MessageTypes.EmergencyClear,
            Data = new
            {
                broadcastId = broadcast.Id
            }
        };

        // Clear from specific clients if targeted
        if (broadcast.TargetClientIds?.Any() == true)
        {
            foreach (var clientId in broadcast.TargetClientIds)
            {
                message.ClientId = clientId;
                await _webSocketServer.SendMessageAsync(clientId, message, cancellationToken);
            }
        }
        else
        {
            await _webSocketServer.BroadcastMessageAsync(message, cancellationToken);
        }

        _logger.LogInformation("Emergency broadcast cleared: {Title}", broadcast.Title);
    }

    public async Task ClearAllBroadcastsAsync(CancellationToken cancellationToken = default)
    {
        var activeBroadcasts = await GetActiveBroadcastsAsync(cancellationToken);
        
        foreach (var broadcast in activeBroadcasts)
        {
            broadcast.IsActive = false;
        }

        var message = new WebSocketMessage
        {
            Type = MessageTypes.EmergencyClear,
            Data = new
            {
                clearAll = true
            }
        };

        await _webSocketServer.BroadcastMessageAsync(message, cancellationToken);
        
        _logger.LogInformation("All emergency broadcasts cleared");
    }

    private WebSocketMessage CreateBroadcastMessage(EmergencyBroadcast broadcast)
    {
        return new WebSocketMessage
        {
            Type = MessageTypes.EmergencyBroadcast,
            Data = new
            {
                id = broadcast.Id,
                title = broadcast.Title,
                message = broadcast.Message,
                priority = broadcast.Priority.ToString(),
                type = broadcast.Type.ToString(),
                expiresAt = broadcast.ExpiresAt?.ToString("o"),
                style = new
                {
                    backgroundColor = broadcast.Style.BackgroundColor,
                    textColor = broadcast.Style.TextColor,
                    fontFamily = broadcast.Style.FontFamily,
                    fontSize = broadcast.Style.FontSize,
                    showFlashing = broadcast.Style.ShowFlashing,
                    showFullScreen = broadcast.Style.ShowFullScreen,
                    displayDuration = broadcast.Style.DisplayDuration
                }
            }
        };
    }
}
