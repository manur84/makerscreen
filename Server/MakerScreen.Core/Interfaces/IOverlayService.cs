using MakerScreen.Core.Models;

namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for managing overlays
/// </summary>
public interface IOverlayService
{
    Task<Overlay> CreateOverlayAsync(Overlay overlay, CancellationToken cancellationToken = default);
    Task<Overlay?> GetOverlayAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Overlay>> GetAllOverlaysAsync(CancellationToken cancellationToken = default);
    Task<Overlay> UpdateOverlayAsync(Overlay overlay, CancellationToken cancellationToken = default);
    Task<bool> DeleteOverlayAsync(string id, CancellationToken cancellationToken = default);
    Task<string> RenderOverlayAsync(string overlayId, CancellationToken cancellationToken = default);
    Task AssignOverlayToClientsAsync(string overlayId, IEnumerable<string> clientIds, CancellationToken cancellationToken = default);
}
