namespace MakerScreen.Core.Models;

/// <summary>
/// Represents an overlay that can be displayed on top of content
/// </summary>
public class Overlay
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public OverlayType Type { get; set; }
    public OverlayPosition Position { get; set; } = new();
    public string Content { get; set; } = string.Empty;
    public string? SqlQuery { get; set; }
    public string? DataSource { get; set; }
    public int RefreshInterval { get; set; } = 60; // seconds
    public OverlayStyle Style { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum OverlayType
{
    Text,
    DateTime,
    Weather,
    Ticker,
    SqlData,
    Html,
    QrCode,
    Logo
}

/// <summary>
/// Position and size of an overlay
/// </summary>
public class OverlayPosition
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 50;
    public AnchorPoint Anchor { get; set; } = AnchorPoint.TopLeft;
}

public enum AnchorPoint
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

/// <summary>
/// Visual styling for overlays
/// </summary>
public class OverlayStyle
{
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 24;
    public string FontColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#00000080";
    public int BorderRadius { get; set; } = 5;
    public int Padding { get; set; } = 10;
    public bool Shadow { get; set; } = true;
}
