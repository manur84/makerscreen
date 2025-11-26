using Xunit;
using MakerScreen.Core.Models;
using FluentAssertions;

namespace MakerScreen.Tests;

public class OverlayTests
{
    // Test data constants - clearly marked as test data, not real credentials
    private const string TestPassword = "test_password_for_unit_testing";
    private const string TestApiKey = "test_api_key_abc123";
    private const string TestUsername = "test_user";
    
    [Fact]
    public void Overlay_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var overlay = new Overlay();

        // Assert
        overlay.Id.Should().NotBeNullOrEmpty();
        overlay.Name.Should().BeEmpty();
        overlay.IsActive.Should().BeTrue();
        overlay.RefreshInterval.Should().Be(60);
        overlay.Position.Should().NotBeNull();
        overlay.Style.Should().NotBeNull();
    }

    [Fact]
    public void OverlayPosition_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var position = new OverlayPosition();

        // Assert
        position.X.Should().Be(0);
        position.Y.Should().Be(0);
        position.Width.Should().Be(200);
        position.Height.Should().Be(50);
        position.Anchor.Should().Be(AnchorPoint.TopLeft);
    }

    [Fact]
    public void OverlayStyle_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var style = new OverlayStyle();

        // Assert
        style.FontFamily.Should().Be("Arial");
        style.FontSize.Should().Be(24);
        style.FontColor.Should().Be("#FFFFFF");
        style.BackgroundColor.Should().Be("#00000080");
        style.BorderRadius.Should().Be(5);
        style.Padding.Should().Be(10);
        style.Shadow.Should().BeTrue();
    }

    [Fact]
    public void Overlay_ShouldSupportSqlData()
    {
        // Arrange & Act
        var overlay = new Overlay
        {
            Name = "Production Stats",
            Type = OverlayType.SqlData,
            SqlQuery = "SELECT COUNT(*) FROM Orders WHERE Date = TODAY",
            DataSource = "ProductionDB",
            RefreshInterval = 30
        };

        // Assert
        overlay.Type.Should().Be(OverlayType.SqlData);
        overlay.SqlQuery.Should().NotBeNullOrEmpty();
        overlay.DataSource.Should().Be("ProductionDB");
        overlay.RefreshInterval.Should().Be(30);
    }

    [Fact]
    public void Overlay_AllTypesShouldBeValid()
    {
        // Arrange & Act
        var types = Enum.GetValues<OverlayType>();

        // Assert
        types.Should().Contain(OverlayType.Text);
        types.Should().Contain(OverlayType.DateTime);
        types.Should().Contain(OverlayType.Weather);
        types.Should().Contain(OverlayType.Ticker);
        types.Should().Contain(OverlayType.SqlData);
        types.Should().Contain(OverlayType.Html);
        types.Should().Contain(OverlayType.QrCode);
        types.Should().Contain(OverlayType.Logo);
    }

    #region SqlConnectionSettings Tests

    [Fact]
    public void SqlConnectionSettings_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var settings = new SqlConnectionSettings();

        // Assert
        settings.Server.Should().BeEmpty();
        settings.Database.Should().BeEmpty();
        settings.Username.Should().BeEmpty();
        settings.Password.Should().BeEmpty();
        settings.Port.Should().Be(1433);
        settings.UseTrustedConnection.Should().BeFalse();
        settings.ConnectionTimeout.Should().Be(30);
    }

    [Fact]
    public void SqlConnectionSettings_ShouldGenerateConnectionString_WithSqlAuth()
    {
        // Arrange
        var settings = new SqlConnectionSettings
        {
            Server = "localhost",
            Database = "TestDB",
            Username = TestUsername,
            Password = TestPassword,
            Port = 1433,
            UseTrustedConnection = false,
            ConnectionTimeout = 30
        };

        // Act
        var connectionString = settings.ToConnectionString();

        // Assert
        connectionString.Should().Contain("Server=localhost,1433");
        connectionString.Should().Contain("Database=TestDB");
        connectionString.Should().Contain($"User Id={TestUsername}");
        connectionString.Should().Contain($"Password={TestPassword}");
        connectionString.Should().Contain("Connection Timeout=30");
        connectionString.Should().NotContain("Trusted_Connection");
    }

    [Fact]
    public void SqlConnectionSettings_ShouldGenerateConnectionString_WithTrustedConnection()
    {
        // Arrange
        var settings = new SqlConnectionSettings
        {
            Server = "dbserver.local",
            Database = "ProductionDB",
            Port = 1433,
            UseTrustedConnection = true,
            ConnectionTimeout = 60
        };

        // Act
        var connectionString = settings.ToConnectionString();

        // Assert
        connectionString.Should().Contain("Server=dbserver.local,1433");
        connectionString.Should().Contain("Database=ProductionDB");
        connectionString.Should().Contain("Trusted_Connection=True");
        connectionString.Should().Contain("Connection Timeout=60");
        connectionString.Should().NotContain("User Id");
        connectionString.Should().NotContain("Password");
    }

    [Fact]
    public void SqlConnectionSettings_ShouldSupportCustomPort()
    {
        // Arrange
        var settings = new SqlConnectionSettings
        {
            Server = "192.168.1.100",
            Database = "AppDB",
            Username = TestUsername,
            Password = TestPassword,
            Port = 1444,
            UseTrustedConnection = false
        };

        // Act
        var connectionString = settings.ToConnectionString();

        // Assert
        connectionString.Should().Contain("Server=192.168.1.100,1444");
    }

    [Fact]
    public void Overlay_ShouldSupportSqlConnectionSettings()
    {
        // Arrange & Act
        var overlay = new Overlay
        {
            Name = "Sales Dashboard",
            Type = OverlayType.SqlData,
            SqlQuery = "SELECT SUM(Amount) FROM Sales WHERE Date = GETDATE()",
            SqlConnectionSettings = new SqlConnectionSettings
            {
                Server = "sql.example.com",
                Database = "SalesDB",
                Username = TestUsername,
                Password = TestPassword,
                Port = 1433
            }
        };

        // Assert
        overlay.SqlConnectionSettings.Should().NotBeNull();
        overlay.SqlConnectionSettings!.Server.Should().Be("sql.example.com");
        overlay.SqlConnectionSettings.Database.Should().Be("SalesDB");
    }

    #endregion

    #region WeatherSettings Tests

    [Fact]
    public void WeatherSettings_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var settings = new WeatherSettings();

        // Assert
        settings.ApiKey.Should().BeEmpty();
        settings.Location.Should().BeEmpty();
        settings.Units.Should().Be("metric");
        settings.Language.Should().Be("en");
        settings.ShowForecast.Should().BeFalse();
        settings.ForecastDays.Should().Be(3);
    }

    [Fact]
    public void WeatherSettings_ShouldSupportAllUnits()
    {
        // Arrange
        var metricSettings = new WeatherSettings { Units = "metric" };
        var imperialSettings = new WeatherSettings { Units = "imperial" };
        var kelvinSettings = new WeatherSettings { Units = "kelvin" };

        // Assert
        metricSettings.Units.Should().Be("metric");
        imperialSettings.Units.Should().Be("imperial");
        kelvinSettings.Units.Should().Be("kelvin");
    }

    [Fact]
    public void WeatherSettings_ShouldSupportForecast()
    {
        // Arrange & Act
        var settings = new WeatherSettings
        {
            ApiKey = TestApiKey,
            Location = "Berlin, DE",
            ShowForecast = true,
            ForecastDays = 5
        };

        // Assert
        settings.ShowForecast.Should().BeTrue();
        settings.ForecastDays.Should().Be(5);
    }

    [Fact]
    public void Overlay_ShouldSupportWeatherSettings()
    {
        // Arrange & Act
        var overlay = new Overlay
        {
            Name = "Weather Widget",
            Type = OverlayType.Weather,
            WeatherSettings = new WeatherSettings
            {
                ApiKey = TestApiKey,
                Location = "Munich",
                Units = "metric",
                Language = "de",
                ShowForecast = true,
                ForecastDays = 7
            }
        };

        // Assert
        overlay.WeatherSettings.Should().NotBeNull();
        overlay.WeatherSettings!.ApiKey.Should().Be(TestApiKey);
        overlay.WeatherSettings.Location.Should().Be("Munich");
        overlay.WeatherSettings.Language.Should().Be("de");
    }

    #endregion

    #region TickerSettings Tests

    [Fact]
    public void TickerSettings_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var settings = new TickerSettings();

        // Assert
        settings.Speed.Should().Be(50);
        settings.Direction.Should().Be("left");
        settings.SourceUrl.Should().BeEmpty();
        settings.Loop.Should().BeTrue();
        settings.ItemSeparator.Should().Be("  •  ");
        settings.PauseDuration.Should().Be(0);
    }

    [Fact]
    public void TickerSettings_ShouldSupportAllDirections()
    {
        // Arrange
        var directions = new[] { "left", "right", "up", "down" };

        // Act & Assert
        foreach (var direction in directions)
        {
            var settings = new TickerSettings { Direction = direction };
            settings.Direction.Should().Be(direction);
        }
    }

    [Fact]
    public void TickerSettings_ShouldSupportRssFeed()
    {
        // Arrange & Act
        var settings = new TickerSettings
        {
            SourceUrl = "https://news.example.com/rss/feed.xml",
            Speed = 75,
            Direction = "left",
            ItemSeparator = " | "
        };

        // Assert
        settings.SourceUrl.Should().Be("https://news.example.com/rss/feed.xml");
        settings.Speed.Should().Be(75);
        settings.ItemSeparator.Should().Be(" | ");
    }

    [Fact]
    public void TickerSettings_ShouldSupportPauseDuration()
    {
        // Arrange & Act
        var settings = new TickerSettings
        {
            Loop = true,
            PauseDuration = 2000  // 2 seconds pause
        };

        // Assert
        settings.Loop.Should().BeTrue();
        settings.PauseDuration.Should().Be(2000);
    }

    [Fact]
    public void Overlay_ShouldSupportTickerSettings()
    {
        // Arrange & Act
        var overlay = new Overlay
        {
            Name = "News Ticker",
            Type = OverlayType.Ticker,
            TickerSettings = new TickerSettings
            {
                Speed = 100,
                Direction = "right",
                SourceUrl = "https://api.example.com/headlines",
                Loop = true,
                ItemSeparator = " *** ",
                PauseDuration = 1000
            }
        };

        // Assert
        overlay.TickerSettings.Should().NotBeNull();
        overlay.TickerSettings!.Speed.Should().Be(100);
        overlay.TickerSettings.Direction.Should().Be("right");
        overlay.TickerSettings.SourceUrl.Should().Be("https://api.example.com/headlines");
    }

    #endregion

    #region Combined Settings Tests

    [Fact]
    public void Overlay_ShouldHaveNullSettingsByDefault()
    {
        // Arrange & Act
        var overlay = new Overlay();

        // Assert
        overlay.SqlConnectionSettings.Should().BeNull();
        overlay.WeatherSettings.Should().BeNull();
        overlay.TickerSettings.Should().BeNull();
    }

    [Fact]
    public void Overlay_ShouldOnlyHaveRelevantSettingsPopulated()
    {
        // Arrange & Act
        var sqlOverlay = new Overlay
        {
            Type = OverlayType.SqlData,
            SqlConnectionSettings = new SqlConnectionSettings { Server = "localhost" }
        };

        var weatherOverlay = new Overlay
        {
            Type = OverlayType.Weather,
            WeatherSettings = new WeatherSettings { Location = "Berlin" }
        };

        var tickerOverlay = new Overlay
        {
            Type = OverlayType.Ticker,
            TickerSettings = new TickerSettings { Speed = 60 }
        };

        // Assert - Each overlay type should only have its relevant settings populated
        sqlOverlay.SqlConnectionSettings.Should().NotBeNull();
        sqlOverlay.WeatherSettings.Should().BeNull();
        sqlOverlay.TickerSettings.Should().BeNull();

        weatherOverlay.SqlConnectionSettings.Should().BeNull();
        weatherOverlay.WeatherSettings.Should().NotBeNull();
        weatherOverlay.TickerSettings.Should().BeNull();

        tickerOverlay.SqlConnectionSettings.Should().BeNull();
        tickerOverlay.WeatherSettings.Should().BeNull();
        tickerOverlay.TickerSettings.Should().NotBeNull();
    }

    #endregion
}
