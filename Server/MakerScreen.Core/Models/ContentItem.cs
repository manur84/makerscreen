namespace MakerScreen.Core.Models;

/// <summary>
/// Represents content to be displayed on signage clients
/// </summary>
public class ContentItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ContentType Type { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string MimeType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Duration { get; set; } = 10; // seconds
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum ContentType
{
    Image,
    Video,
    Html,
    Url
}
