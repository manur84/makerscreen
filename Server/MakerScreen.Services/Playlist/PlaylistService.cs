using System.Collections.Concurrent;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.Playlist;

/// <summary>
/// Manages playlists and their scheduling
/// </summary>
public class PlaylistService : IPlaylistService
{
    private readonly ILogger<PlaylistService> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly IContentService _contentService;
    private readonly ConcurrentDictionary<string, Core.Models.Playlist> _playlists = new();
    private readonly ConcurrentDictionary<string, string> _clientPlaylistAssignments = new();

    public PlaylistService(
        ILogger<PlaylistService> logger,
        IWebSocketServer webSocketServer,
        IContentService contentService)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _contentService = contentService;
    }

    public Task<Core.Models.Playlist> CreatePlaylistAsync(Core.Models.Playlist playlist, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating playlist: {Name}", playlist.Name);
        
        _playlists.TryAdd(playlist.Id, playlist);
        
        return Task.FromResult(playlist);
    }

    public Task<Core.Models.Playlist?> GetPlaylistAsync(string id, CancellationToken cancellationToken = default)
    {
        _playlists.TryGetValue(id, out var playlist);
        return Task.FromResult(playlist);
    }

    public Task<IEnumerable<Core.Models.Playlist>> GetAllPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Core.Models.Playlist>>(_playlists.Values.ToList());
    }

    public Task<Core.Models.Playlist> UpdatePlaylistAsync(Core.Models.Playlist playlist, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating playlist: {Id}", playlist.Id);
        
        playlist.UpdatedAt = DateTime.UtcNow;
        _playlists[playlist.Id] = playlist;
        
        return Task.FromResult(playlist);
    }

    public Task<bool> DeletePlaylistAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting playlist: {Id}", id);
        
        return Task.FromResult(_playlists.TryRemove(id, out _));
    }

    public async Task AssignPlaylistToClientsAsync(string playlistId, IEnumerable<string> clientIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assigning playlist {PlaylistId} to clients", playlistId);
        
        var playlist = await GetPlaylistAsync(playlistId, cancellationToken);
        if (playlist == null)
        {
            _logger.LogWarning("Playlist {PlaylistId} not found", playlistId);
            return;
        }

        foreach (var clientId in clientIds)
        {
            _clientPlaylistAssignments[clientId] = playlistId;
            
            // Notify client of playlist update
            var message = new WebSocketMessage
            {
                Type = MessageTypes.ContentUpdate,
                ClientId = clientId,
                Data = new
                {
                    action = "playlist_assigned",
                    playlistId,
                    playlist = SerializePlaylist(playlist)
                }
            };
            
            await _webSocketServer.SendMessageAsync(clientId, message, cancellationToken);
        }
    }

    public Task<Core.Models.Playlist?> GetActivePlaylistForClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (_clientPlaylistAssignments.TryGetValue(clientId, out var playlistId))
        {
            return GetPlaylistAsync(playlistId, cancellationToken);
        }
        
        // Return default playlist if exists
        var defaultPlaylist = _playlists.Values.FirstOrDefault(p => p.IsActive);
        return Task.FromResult(defaultPlaylist);
    }

    public bool IsPlaylistActive(Core.Models.Playlist playlist)
    {
        if (!playlist.IsActive) return false;
        
        var now = DateTime.UtcNow;
        var schedule = playlist.Schedule;
        
        if (schedule == null) return true;

        // Check date range
        if (schedule.StartDate.HasValue && now < schedule.StartDate.Value) return false;
        if (schedule.EndDate.HasValue && now > schedule.EndDate.Value) return false;

        // Check day of week
        if (!schedule.ActiveDays.Contains(now.DayOfWeek)) return false;

        // Check time range
        var currentTime = now.TimeOfDay;
        if (schedule.StartTime.HasValue && currentTime < schedule.StartTime.Value) return false;
        if (schedule.EndTime.HasValue && currentTime > schedule.EndTime.Value) return false;

        return true;
    }

    private object SerializePlaylist(Core.Models.Playlist playlist)
    {
        return new
        {
            id = playlist.Id,
            name = playlist.Name,
            items = playlist.Items.Select(i => new
            {
                order = i.Order,
                contentId = i.ContentId,
                duration = i.Duration,
                transition = i.Transition.ToString(),
                transitionDuration = i.TransitionDuration
            }).ToList()
        };
    }
}
