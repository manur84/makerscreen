using MakerScreen.Core.Models;

namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for managing emergency broadcasts
/// </summary>
public interface IEmergencyBroadcastService
{
    Task<EmergencyBroadcast> CreateBroadcastAsync(EmergencyBroadcast broadcast, CancellationToken cancellationToken = default);
    Task<EmergencyBroadcast?> GetBroadcastAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<EmergencyBroadcast>> GetAllBroadcastsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<EmergencyBroadcast>> GetActiveBroadcastsAsync(CancellationToken cancellationToken = default);
    Task<EmergencyBroadcast> UpdateBroadcastAsync(EmergencyBroadcast broadcast, CancellationToken cancellationToken = default);
    Task<bool> DeleteBroadcastAsync(string id, CancellationToken cancellationToken = default);
    Task SendBroadcastAsync(string broadcastId, CancellationToken cancellationToken = default);
    Task SendBroadcastToGroupAsync(string broadcastId, string groupId, CancellationToken cancellationToken = default);
    Task ClearBroadcastAsync(string broadcastId, CancellationToken cancellationToken = default);
    Task ClearAllBroadcastsAsync(CancellationToken cancellationToken = default);
}
