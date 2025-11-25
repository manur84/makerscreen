using System.Collections.Concurrent;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.Group;

/// <summary>
/// Manages client groups for targeted content delivery
/// </summary>
public class ClientGroupService : IClientGroupService
{
    private readonly ILogger<ClientGroupService> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly IPlaylistService _playlistService;
    private readonly IContentService _contentService;
    private readonly ConcurrentDictionary<string, ClientGroup> _groups = new();

    public ClientGroupService(
        ILogger<ClientGroupService> logger,
        IWebSocketServer webSocketServer,
        IPlaylistService playlistService,
        IContentService contentService)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _playlistService = playlistService;
        _contentService = contentService;
    }

    public Task<ClientGroup> CreateGroupAsync(ClientGroup group, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating client group: {Name}", group.Name);
        
        _groups.TryAdd(group.Id, group);
        
        return Task.FromResult(group);
    }

    public Task<ClientGroup?> GetGroupAsync(string id, CancellationToken cancellationToken = default)
    {
        _groups.TryGetValue(id, out var group);
        return Task.FromResult(group);
    }

    public Task<IEnumerable<ClientGroup>> GetAllGroupsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ClientGroup>>(_groups.Values.ToList());
    }

    public Task<ClientGroup> UpdateGroupAsync(ClientGroup group, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating client group: {Id}", group.Id);
        
        _groups[group.Id] = group;
        
        return Task.FromResult(group);
    }

    public Task<bool> DeleteGroupAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting client group: {Id}", id);
        
        return Task.FromResult(_groups.TryRemove(id, out _));
    }

    public Task AddClientToGroupAsync(string groupId, string clientId, CancellationToken cancellationToken = default)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            if (!group.ClientIds.Contains(clientId))
            {
                group.ClientIds.Add(clientId);
                _logger.LogInformation("Added client {ClientId} to group {GroupId}", clientId, groupId);
            }
        }
        
        return Task.CompletedTask;
    }

    public Task RemoveClientFromGroupAsync(string groupId, string clientId, CancellationToken cancellationToken = default)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            group.ClientIds.Remove(clientId);
            _logger.LogInformation("Removed client {ClientId} from group {GroupId}", clientId, groupId);
        }
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ClientGroup>> GetGroupsForClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var clientGroups = _groups.Values.Where(g => g.ClientIds.Contains(clientId)).ToList();
        return Task.FromResult<IEnumerable<ClientGroup>>(clientGroups);
    }

    public async Task AssignPlaylistToGroupAsync(string groupId, string playlistId, CancellationToken cancellationToken = default)
    {
        if (!_groups.TryGetValue(groupId, out var group))
        {
            _logger.LogWarning("Group {GroupId} not found", groupId);
            return;
        }

        group.DefaultPlaylistId = playlistId;
        
        // Assign playlist to all clients in the group
        await _playlistService.AssignPlaylistToClientsAsync(playlistId, group.ClientIds, cancellationToken);
        
        _logger.LogInformation("Assigned playlist {PlaylistId} to group {GroupId}", playlistId, groupId);
    }

    public async Task PushContentToGroupAsync(string groupId, string contentId, CancellationToken cancellationToken = default)
    {
        if (!_groups.TryGetValue(groupId, out var group))
        {
            _logger.LogWarning("Group {GroupId} not found", groupId);
            return;
        }

        var content = await _contentService.GetContentAsync(contentId);
        if (content == null)
        {
            _logger.LogWarning("Content {ContentId} not found", contentId);
            return;
        }

        foreach (var clientId in group.ClientIds)
        {
            var message = new WebSocketMessage
            {
                Type = MessageTypes.ContentUpdate,
                ClientId = clientId,
                Data = new
                {
                    contentId = content.Id,
                    name = content.Name,
                    type = content.Type.ToString(),
                    mimeType = content.MimeType,
                    data = Convert.ToBase64String(content.Data)
                }
            };

            await _webSocketServer.SendMessageAsync(clientId, message, cancellationToken);
        }

        _logger.LogInformation("Pushed content {ContentId} to group {GroupId} ({ClientCount} clients)", 
            contentId, groupId, group.ClientIds.Count);
    }
}
