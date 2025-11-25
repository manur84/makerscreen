namespace MakerScreen.Core.Models;

/// <summary>
/// Groups clients for targeted content delivery
/// </summary>
public class ClientGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ClientIds { get; set; } = new();
    public string? DefaultPlaylistId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
