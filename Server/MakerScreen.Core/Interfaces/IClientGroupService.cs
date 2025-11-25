using MakerScreen.Core.Models;

namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for managing client groups
/// </summary>
public interface IClientGroupService
{
    Task<ClientGroup> CreateGroupAsync(ClientGroup group, CancellationToken cancellationToken = default);
    Task<ClientGroup?> GetGroupAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ClientGroup>> GetAllGroupsAsync(CancellationToken cancellationToken = default);
    Task<ClientGroup> UpdateGroupAsync(ClientGroup group, CancellationToken cancellationToken = default);
    Task<bool> DeleteGroupAsync(string id, CancellationToken cancellationToken = default);
    Task AddClientToGroupAsync(string groupId, string clientId, CancellationToken cancellationToken = default);
    Task RemoveClientFromGroupAsync(string groupId, string clientId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ClientGroup>> GetGroupsForClientAsync(string clientId, CancellationToken cancellationToken = default);
    Task AssignPlaylistToGroupAsync(string groupId, string playlistId, CancellationToken cancellationToken = default);
    Task PushContentToGroupAsync(string groupId, string contentId, CancellationToken cancellationToken = default);
}
