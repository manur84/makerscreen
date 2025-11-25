using MakerScreen.Core.Models;

namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for managing playlists
/// </summary>
public interface IPlaylistService
{
    Task<Playlist> CreatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken = default);
    Task<Playlist?> GetPlaylistAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Playlist>> GetAllPlaylistsAsync(CancellationToken cancellationToken = default);
    Task<Playlist> UpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken = default);
    Task<bool> DeletePlaylistAsync(string id, CancellationToken cancellationToken = default);
    Task AssignPlaylistToClientsAsync(string playlistId, IEnumerable<string> clientIds, CancellationToken cancellationToken = default);
    Task<Playlist?> GetActivePlaylistForClientAsync(string clientId, CancellationToken cancellationToken = default);
}
