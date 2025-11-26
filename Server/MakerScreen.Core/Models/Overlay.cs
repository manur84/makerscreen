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

    /// <summary>
    /// SQL connection settings for SqlData type overlays
    /// </summary>
    public SqlConnectionSettings? SqlConnectionSettings { get; set; }

    /// <summary>
    /// Weather settings for Weather type overlays
    /// </summary>
    public WeatherSettings? WeatherSettings { get; set; }

    /// <summary>
    /// Ticker settings for Ticker type overlays
    /// </summary>
    public TickerSettings? TickerSettings { get; set; }
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

/// <summary>
/// SQL Server connection settings for SqlData type overlays
/// </summary>
public class SqlConnectionSettings
{
    /// <summary>
    /// SQL Server hostname or IP address
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// Database name to connect to
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Username for SQL authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for SQL authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// SQL Server port (default: 1433)
    /// </summary>
    public int Port { get; set; } = 1433;

    /// <summary>
    /// Whether to use Windows Integrated Authentication
    /// </summary>
    public bool UseTrustedConnection { get; set; } = false;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Generates a connection string from the settings
    /// </summary>
    public string ToConnectionString()
    {
        if (UseTrustedConnection)
        {
            return $"Server={Server},{Port};Database={Database};Trusted_Connection=True;Connection Timeout={ConnectionTimeout}";
        }
        return $"Server={Server},{Port};Database={Database};User Id={Username};Password={Password};Connection Timeout={ConnectionTimeout}";
    }
}

/// <summary>
/// Weather API settings for Weather type overlays
/// </summary>
public class WeatherSettings
{
    /// <summary>
    /// API key for weather service (e.g., OpenWeatherMap)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Location for weather data (city name or coordinates)
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Temperature units: "metric" (Celsius), "imperial" (Fahrenheit), or "kelvin"
    /// </summary>
    public string Units { get; set; } = "metric";

    /// <summary>
    /// Language code for weather descriptions (e.g., "en", "de", "fr")
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Whether to show extended forecast
    /// </summary>
    public bool ShowForecast { get; set; } = false;

    /// <summary>
    /// Number of forecast days to show (1-7)
    /// </summary>
    public int ForecastDays { get; set; } = 3;
}

/// <summary>
/// Ticker settings for Ticker type overlays (scrolling text)
/// </summary>
public class TickerSettings
{
    /// <summary>
    /// Scroll speed in pixels per second
    /// </summary>
    public int Speed { get; set; } = 50;

    /// <summary>
    /// Scroll direction: "left", "right", "up", "down"
    /// </summary>
    public string Direction { get; set; } = "left";

    /// <summary>
    /// URL to fetch ticker content from (RSS feed or API endpoint)
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether to loop the ticker content
    /// </summary>
    public bool Loop { get; set; } = true;

    /// <summary>
    /// Separator between ticker items
    /// </summary>
    public string ItemSeparator { get; set; } = "  •  ";

    /// <summary>
    /// Pause duration in milliseconds when ticker reaches the end (before looping)
    /// </summary>
    public int PauseDuration { get; set; } = 0;
}
