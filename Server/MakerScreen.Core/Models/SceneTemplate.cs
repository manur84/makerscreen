namespace MakerScreen.Core.Models;

/// <summary>
/// Represents a predefined scene template for quick scene creation
/// </summary>
public class SceneTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string ThumbnailIcon { get; set; } = "📄";
    
    /// <summary>
    /// Resolution preset for this template
    /// </summary>
    public CompositionResolution Resolution { get; set; } = new();
    
    /// <summary>
    /// Background settings for this template
    /// </summary>
    public CompositionBackground Background { get; set; } = new();
    
    /// <summary>
    /// Predefined overlay placements for this template
    /// </summary>
    public List<TemplateOverlayPlacement> OverlayPlacements { get; set; } = new();
}

/// <summary>
/// Represents a predefined overlay placement in a template
/// </summary>
public class TemplateOverlayPlacement
{
    public string Name { get; set; } = string.Empty;
    public OverlayType OverlayType { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 50;
    public int ZIndex { get; set; }
    public string DefaultContent { get; set; } = string.Empty;
    public OverlayStyle Style { get; set; } = new();
}

/// <summary>
/// Common widget types for quick overlay creation
/// </summary>
public enum WidgetType
{
    DateTime,
    Clock,
    Weather,
    Ticker,
    Logo,
    QrCode,
    SocialFeed,
    Counter,
    ProgressBar,
    Calendar
}

/// <summary>
/// Represents a widget configuration for quick addition to scenes
/// </summary>
public class Widget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public WidgetType Type { get; set; }
    public string Icon { get; set; } = "📦";
    public string Description { get; set; } = string.Empty;
    public int DefaultWidth { get; set; } = 200;
    public int DefaultHeight { get; set; } = 50;
    public Dictionary<string, string> DefaultSettings { get; set; } = new();
}

/// <summary>
/// Predefined scene templates
/// </summary>
public static class DefaultSceneTemplates
{
    public static List<SceneTemplate> GetTemplates()
    {
        return new List<SceneTemplate>
        {
            new SceneTemplate
            {
                Name = "Blank Canvas",
                Description = "Empty scene with black background",
                Category = "Basic",
                ThumbnailIcon = "⬛",
                Resolution = new CompositionResolution { Width = 1920, Height = 1080 },
                Background = new CompositionBackground { Type = BackgroundType.Color, Color = "#000000" }
            },
            new SceneTemplate
            {
                Name = "Welcome Screen",
                Description = "Welcome message with date/time",
                Category = "Corporate",
                ThumbnailIcon = "👋",
                Resolution = new CompositionResolution { Width = 1920, Height = 1080 },
                Background = new CompositionBackground { Type = BackgroundType.Color, Color = "#1a237e" },
                OverlayPlacements = new List<TemplateOverlayPlacement>
                {
                    new TemplateOverlayPlacement
                    {
                        Name = "Welcome Text",
                        OverlayType = OverlayType.Text,
                        X = 100, Y = 400,
                        Width = 800, Height = 100,
                        DefaultContent = "Welcome",
                        Style = new OverlayStyle { FontSize = 72, FontColor = "#FFFFFF" }
                    },
                    new TemplateOverlayPlacement
                    {
                        Name = "Date/Time",
                        OverlayType = OverlayType.DateTime,
                        X = 1600, Y = 50,
                        Width = 280, Height = 60,
                        DefaultContent = "HH:mm",
                        Style = new OverlayStyle { FontSize = 36, FontColor = "#FFFFFF" }
                    }
                }
            },
            new SceneTemplate
            {
                Name = "Menu Board",
                Description = "Restaurant menu layout",
                Category = "Retail",
                ThumbnailIcon = "🍽️",
                Resolution = new CompositionResolution { Width = 1080, Height = 1920 },
                Background = new CompositionBackground { Type = BackgroundType.Color, Color = "#2d2d2d" },
                OverlayPlacements = new List<TemplateOverlayPlacement>
                {
                    new TemplateOverlayPlacement
                    {
                        Name = "Header",
                        OverlayType = OverlayType.Text,
                        X = 40, Y = 40,
                        Width = 1000, Height = 80,
                        DefaultContent = "Today's Menu",
                        Style = new OverlayStyle { FontSize = 48, FontColor = "#FFD700" }
                    }
                }
            },
            new SceneTemplate
            {
                Name = "Info Display",
                Description = "Information display with ticker",
                Category = "Corporate",
                ThumbnailIcon = "ℹ️",
                Resolution = new CompositionResolution { Width = 1920, Height = 1080 },
                Background = new CompositionBackground { Type = BackgroundType.Color, Color = "#263238" },
                OverlayPlacements = new List<TemplateOverlayPlacement>
                {
                    new TemplateOverlayPlacement
                    {
                        Name = "News Ticker",
                        OverlayType = OverlayType.Ticker,
                        X = 0, Y = 1020,
                        Width = 1920, Height = 60,
                        DefaultContent = "Breaking news and updates scroll here...",
                        Style = new OverlayStyle { FontSize = 28, FontColor = "#FFFFFF", BackgroundColor = "#1565C0" }
                    },
                    new TemplateOverlayPlacement
                    {
                        Name = "Clock",
                        OverlayType = OverlayType.DateTime,
                        X = 1700, Y = 20,
                        Width = 200, Height = 50,
                        DefaultContent = "HH:mm:ss",
                        Style = new OverlayStyle { FontSize = 32, FontColor = "#FFFFFF" }
                    }
                }
            },
            new SceneTemplate
            {
                Name = "Event Display",
                Description = "Event announcement layout",
                Category = "Events",
                ThumbnailIcon = "🎉",
                Resolution = new CompositionResolution { Width = 1920, Height = 1080 },
                Background = new CompositionBackground { Type = BackgroundType.Color, Color = "#4a148c" },
                OverlayPlacements = new List<TemplateOverlayPlacement>
                {
                    new TemplateOverlayPlacement
                    {
                        Name = "Event Title",
                        OverlayType = OverlayType.Text,
                        X = 100, Y = 200,
                        Width = 1720, Height = 120,
                        DefaultContent = "Event Name",
                        Style = new OverlayStyle { FontSize = 72, FontColor = "#FFFFFF" }
                    },
                    new TemplateOverlayPlacement
                    {
                        Name = "Event Date",
                        OverlayType = OverlayType.Text,
                        X = 100, Y = 350,
                        Width = 800, Height = 60,
                        DefaultContent = "Date & Time",
                        Style = new OverlayStyle { FontSize = 36, FontColor = "#E1BEE7" }
                    }
                }
            },
            new SceneTemplate
            {
                Name = "Social Wall",
                Description = "Social media feed display",
                Category = "Social",
                ThumbnailIcon = "📱",
                Resolution = new CompositionResolution { Width = 1920, Height = 1080 },
                Background = new CompositionBackground { Type = BackgroundType.Color, Color = "#121212" }
            }
        };
    }

    public static List<Widget> GetWidgets()
    {
        return new List<Widget>
        {
            new Widget
            {
                Name = "Digital Clock",
                Type = WidgetType.Clock,
                Icon = "🕐",
                Description = "Shows current time",
                DefaultWidth = 200,
                DefaultHeight = 60,
                DefaultSettings = new Dictionary<string, string>
                {
                    ["format"] = "HH:mm:ss",
                    ["showDate"] = "false"
                }
            },
            new Widget
            {
                Name = "Date & Time",
                Type = WidgetType.DateTime,
                Icon = "📅",
                Description = "Shows date and time",
                DefaultWidth = 300,
                DefaultHeight = 80,
                DefaultSettings = new Dictionary<string, string>
                {
                    ["format"] = "dddd, MMMM d, yyyy HH:mm"
                }
            },
            new Widget
            {
                Name = "Weather",
                Type = WidgetType.Weather,
                Icon = "🌤️",
                Description = "Current weather conditions",
                DefaultWidth = 250,
                DefaultHeight = 100,
                DefaultSettings = new Dictionary<string, string>
                {
                    ["location"] = "auto",
                    ["units"] = "celsius"
                }
            },
            new Widget
            {
                Name = "News Ticker",
                Type = WidgetType.Ticker,
                Icon = "📰",
                Description = "Scrolling news or announcements",
                DefaultWidth = 1920,
                DefaultHeight = 60,
                DefaultSettings = new Dictionary<string, string>
                {
                    ["speed"] = "medium",
                    ["direction"] = "left"
                }
            },
            new Widget
            {
                Name = "Logo",
                Type = WidgetType.Logo,
                Icon = "🏢",
                Description = "Company logo display",
                DefaultWidth = 200,
                DefaultHeight = 100
            },
            new Widget
            {
                Name = "QR Code",
                Type = WidgetType.QrCode,
                Icon = "📲",
                Description = "QR code for links or information",
                DefaultWidth = 150,
                DefaultHeight = 150,
                DefaultSettings = new Dictionary<string, string>
                {
                    ["content"] = "https://example.com"
                }
            },
            new Widget
            {
                Name = "Countdown",
                Type = WidgetType.Counter,
                Icon = "⏱️",
                Description = "Countdown timer to an event",
                DefaultWidth = 300,
                DefaultHeight = 100,
                DefaultSettings = new Dictionary<string, string>
                {
                    ["targetDate"] = "",
                    ["showDays"] = "true"
                }
            },
            new Widget
            {
                Name = "Progress Bar",
                Type = WidgetType.ProgressBar,
                Icon = "📊",
                Description = "Visual progress indicator",
                DefaultWidth = 400,
                DefaultHeight = 40,
                DefaultSettings = new Dictionary<string, string>
                {
                    ["value"] = "50",
                    ["max"] = "100"
                }
            }
        };
    }
}
