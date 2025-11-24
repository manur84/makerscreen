using MakerScreen.Core.Models;

namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for managing content distribution
/// </summary>
public interface IContentService
{
    Task<ContentItem> AddContentAsync(ContentItem content, CancellationToken cancellationToken = default);
    Task<ContentItem?> GetContentAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContentItem>> GetAllContentAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteContentAsync(string id, CancellationToken cancellationToken = default);
    Task PushContentToClientsAsync(string contentId, CancellationToken cancellationToken = default);
}
