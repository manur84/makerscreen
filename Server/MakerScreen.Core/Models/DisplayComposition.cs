namespace MakerScreen.Core.Models;

/// <summary>
/// Represents a complete display composition with resolution, background, and overlays
/// that can be published to displays as a single unit
/// </summary>
public class DisplayComposition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Resolution settings for the composition
    /// </summary>
    public CompositionResolution Resolution { get; set; } = new();
    
    /// <summary>
    /// Background image settings
    /// </summary>
    public CompositionBackground Background { get; set; } = new();
    
    /// <summary>
    /// List of overlay placements within this composition
    /// </summary>
    public List<CompositionOverlay> Overlays { get; set; } = new();
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Resolution settings for a composition
/// </summary>
public class CompositionResolution
{
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    
    /// <summary>
    /// Predefined resolution preset name (e.g., "Full HD", "4K")
    /// </summary>
    public string PresetName { get; set; } = "Full HD (1920x1080)";
    
    /// <summary>
    /// Aspect ratio calculated from width/height
    /// </summary>
    public string AspectRatio => CalculateAspectRatio();
    
    private string CalculateAspectRatio()
    {
        var gcd = GCD(Width, Height);
        return $"{Width / gcd}:{Height / gcd}";
    }
    
    private static int GCD(int a, int b)
    {
        while (b != 0)
        {
            var t = b;
            b = a % b;
            a = t;
        }
        return a;
    }
}

/// <summary>
/// Background settings for a composition
/// </summary>
public class CompositionBackground
{
    /// <summary>
    /// Background type: None, Color, Image
    /// </summary>
    public BackgroundType Type { get; set; } = BackgroundType.Color;
    
    /// <summary>
    /// Background color in hex format (e.g., "#000000")
    /// </summary>
    public string Color { get; set; } = "#000000";
    
    /// <summary>
    /// ID of the content item used as background image
    /// </summary>
    public string? ImageContentId { get; set; }
    
    /// <summary>
    /// Base64-encoded image data for preview
    /// </summary>
    public byte[]? ImageData { get; set; }
    
    /// <summary>
    /// Image scaling mode
    /// </summary>
    public ImageScaleMode ScaleMode { get; set; } = ImageScaleMode.Fill;
}

public enum BackgroundType
{
    None,
    Color,
    Image
}

public enum ImageScaleMode
{
    /// <summary>
    /// Scale to fill entire area (may crop)
    /// </summary>
    Fill,
    
    /// <summary>
    /// Scale to fit entire image (may show letterbox)
    /// </summary>
    Fit,
    
    /// <summary>
    /// Stretch to fill (may distort)
    /// </summary>
    Stretch,
    
    /// <summary>
    /// Center image without scaling
    /// </summary>
    Center,
    
    /// <summary>
    /// Tile image to fill area
    /// </summary>
    Tile
}

/// <summary>
/// Represents an overlay placed within a composition at a specific position
/// </summary>
public class CompositionOverlay
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Reference to the original overlay definition
    /// </summary>
    public string OverlayId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name for display purposes
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// X position in pixels from left
    /// </summary>
    public double X { get; set; }
    
    /// <summary>
    /// Y position in pixels from top
    /// </summary>
    public double Y { get; set; }
    
    /// <summary>
    /// Width of the overlay
    /// </summary>
    public double Width { get; set; } = 200;
    
    /// <summary>
    /// Height of the overlay
    /// </summary>
    public double Height { get; set; } = 50;
    
    /// <summary>
    /// Layer order (higher = on top)
    /// </summary>
    public int ZIndex { get; set; }
    
    /// <summary>
    /// Is this overlay visible in the composition
    /// </summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// Is this overlay locked for editing
    /// </summary>
    public bool IsLocked { get; set; } = false;
}

/// <summary>
/// Common resolution presets
/// </summary>
public static class ResolutionPresets
{
    public static readonly Dictionary<string, (int Width, int Height)> Presets = new()
    {
        { "HD (1280x720)", (1280, 720) },
        { "Full HD (1920x1080)", (1920, 1080) },
        { "4K (3840x2160)", (3840, 2160) },
        { "Portrait HD (720x1280)", (720, 1280) },
        { "Portrait Full HD (1080x1920)", (1080, 1920) },
        { "Portrait 4K (2160x3840)", (2160, 3840) },
        { "Square (1080x1080)", (1080, 1080) },
        { "Ultra Wide (2560x1080)", (2560, 1080) },
        { "Custom", (0, 0) }
    };
}
