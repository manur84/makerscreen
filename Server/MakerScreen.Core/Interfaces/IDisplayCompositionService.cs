using MakerScreen.Core.Models;

namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for managing display compositions (scenes with background and overlays)
/// </summary>
public interface IDisplayCompositionService
{
    /// <summary>
    /// Create a new composition
    /// </summary>
    Task<DisplayComposition> CreateCompositionAsync(DisplayComposition composition, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a composition by ID
    /// </summary>
    Task<DisplayComposition?> GetCompositionAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all compositions
    /// </summary>
    Task<IEnumerable<DisplayComposition>> GetAllCompositionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an existing composition
    /// </summary>
    Task<DisplayComposition> UpdateCompositionAsync(DisplayComposition composition, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a composition
    /// </summary>
    Task<bool> DeleteCompositionAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add an overlay to a composition
    /// </summary>
    Task<DisplayComposition> AddOverlayToCompositionAsync(string compositionId, CompositionOverlay overlay, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove an overlay from a composition
    /// </summary>
    Task<DisplayComposition> RemoveOverlayFromCompositionAsync(string compositionId, string overlayPlacementId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an overlay position/size within a composition
    /// </summary>
    Task<DisplayComposition> UpdateOverlayInCompositionAsync(string compositionId, CompositionOverlay overlay, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set the background for a composition
    /// </summary>
    Task<DisplayComposition> SetBackgroundAsync(string compositionId, CompositionBackground background, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set the resolution for a composition
    /// </summary>
    Task<DisplayComposition> SetResolutionAsync(string compositionId, CompositionResolution resolution, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish a composition to all connected clients
    /// </summary>
    Task PublishCompositionAsync(string compositionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish a composition to specific clients
    /// </summary>
    Task PublishCompositionToClientsAsync(string compositionId, IEnumerable<string> clientIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish a composition to a client group
    /// </summary>
    Task PublishCompositionToGroupAsync(string compositionId, string groupId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Render a composition preview as base64 image
    /// </summary>
    Task<string> RenderCompositionPreviewAsync(string compositionId, CancellationToken cancellationToken = default);
}
