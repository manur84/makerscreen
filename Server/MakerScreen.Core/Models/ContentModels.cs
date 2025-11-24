namespace MakerScreen.Core.Models;

public class Content
{
    public int ContentID { get; set; }
    public Guid ContentGUID { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public byte[]? FileData { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

public class Playlist
{
    public int PlaylistID { get; set; }
    public string PlaylistName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
