using Xunit;
using MakerScreen.Core.Models;

namespace MakerScreen.Tests;

public class SceneTemplateTests
{
    [Fact]
    public void SceneTemplate_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var template = new SceneTemplate();

        // Assert
        Assert.NotNull(template.Id);
        Assert.NotEmpty(template.Id);
        Assert.Equal(string.Empty, template.Name);
        Assert.Equal("General", template.Category);
        Assert.Equal("📄", template.ThumbnailIcon);
        Assert.NotNull(template.Resolution);
        Assert.NotNull(template.Background);
        Assert.NotNull(template.OverlayPlacements);
        Assert.Empty(template.OverlayPlacements);
    }

    [Fact]
    public void TemplateOverlayPlacement_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var placement = new TemplateOverlayPlacement();

        // Assert
        Assert.Equal(string.Empty, placement.Name);
        Assert.Equal(OverlayType.Text, placement.OverlayType);
        Assert.Equal(0, placement.X);
        Assert.Equal(0, placement.Y);
        Assert.Equal(200, placement.Width);
        Assert.Equal(50, placement.Height);
        Assert.Equal(0, placement.ZIndex);
        Assert.NotNull(placement.Style);
    }

    [Fact]
    public void Widget_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var widget = new Widget();

        // Assert
        Assert.NotNull(widget.Id);
        Assert.NotEmpty(widget.Id);
        Assert.Equal(string.Empty, widget.Name);
        Assert.Equal("📦", widget.Icon);
        Assert.Equal(200, widget.DefaultWidth);
        Assert.Equal(50, widget.DefaultHeight);
        Assert.NotNull(widget.DefaultSettings);
        Assert.Empty(widget.DefaultSettings);
    }

    [Fact]
    public void WidgetType_ShouldHaveAllTypes()
    {
        // Assert
        Assert.Equal(10, Enum.GetValues<WidgetType>().Length);
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.DateTime));
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.Clock));
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.Weather));
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.Ticker));
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.Logo));
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.QrCode));
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.SocialFeed));
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.Counter));
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.ProgressBar));
        Assert.True(Enum.IsDefined(typeof(WidgetType), WidgetType.Calendar));
    }

    [Fact]
    public void DefaultSceneTemplates_ShouldReturnTemplates()
    {
        // Act
        var templates = DefaultSceneTemplates.GetTemplates();

        // Assert
        Assert.NotNull(templates);
        Assert.NotEmpty(templates);
        Assert.True(templates.Count >= 5);
    }

    [Fact]
    public void DefaultSceneTemplates_ShouldContainBlankCanvas()
    {
        // Act
        var templates = DefaultSceneTemplates.GetTemplates();
        var blankCanvas = templates.FirstOrDefault(t => t.Name == "Blank Canvas");

        // Assert
        Assert.NotNull(blankCanvas);
        Assert.Equal("Basic", blankCanvas.Category);
        Assert.Equal(1920, blankCanvas.Resolution.Width);
        Assert.Equal(1080, blankCanvas.Resolution.Height);
        Assert.Equal(BackgroundType.Color, blankCanvas.Background.Type);
    }

    [Fact]
    public void DefaultSceneTemplates_ShouldContainWelcomeScreen()
    {
        // Act
        var templates = DefaultSceneTemplates.GetTemplates();
        var welcomeScreen = templates.FirstOrDefault(t => t.Name == "Welcome Screen");

        // Assert
        Assert.NotNull(welcomeScreen);
        Assert.Equal("Corporate", welcomeScreen.Category);
        Assert.NotEmpty(welcomeScreen.OverlayPlacements);
    }

    [Fact]
    public void DefaultWidgets_ShouldReturnWidgets()
    {
        // Act
        var widgets = DefaultSceneTemplates.GetWidgets();

        // Assert
        Assert.NotNull(widgets);
        Assert.NotEmpty(widgets);
        Assert.True(widgets.Count >= 5);
    }

    [Fact]
    public void DefaultWidgets_ShouldContainClock()
    {
        // Act
        var widgets = DefaultSceneTemplates.GetWidgets();
        var clock = widgets.FirstOrDefault(w => w.Type == WidgetType.Clock);

        // Assert
        Assert.NotNull(clock);
        Assert.Equal("Digital Clock", clock.Name);
        Assert.Equal("🕐", clock.Icon);
    }

    [Fact]
    public void DefaultWidgets_ShouldContainTicker()
    {
        // Act
        var widgets = DefaultSceneTemplates.GetWidgets();
        var ticker = widgets.FirstOrDefault(w => w.Type == WidgetType.Ticker);

        // Assert
        Assert.NotNull(ticker);
        Assert.Equal("News Ticker", ticker.Name);
        Assert.Equal(1920, ticker.DefaultWidth);
    }

    [Fact]
    public void SceneTemplate_ShouldSupportOverlayPlacements()
    {
        // Arrange
        var template = new SceneTemplate
        {
            Name = "Test Template",
            OverlayPlacements = new List<TemplateOverlayPlacement>
            {
                new TemplateOverlayPlacement
                {
                    Name = "Header",
                    OverlayType = OverlayType.Text,
                    X = 100,
                    Y = 50,
                    Width = 500,
                    Height = 80
                },
                new TemplateOverlayPlacement
                {
                    Name = "Clock",
                    OverlayType = OverlayType.DateTime,
                    X = 1700,
                    Y = 50,
                    Width = 200,
                    Height = 60
                }
            }
        };

        // Assert
        Assert.Equal(2, template.OverlayPlacements.Count);
        Assert.Equal("Header", template.OverlayPlacements[0].Name);
        Assert.Equal(OverlayType.DateTime, template.OverlayPlacements[1].OverlayType);
    }

    [Fact]
    public void Widget_ShouldSupportDefaultSettings()
    {
        // Arrange
        var widget = new Widget
        {
            Name = "Custom Widget",
            Type = WidgetType.Counter,
            DefaultSettings = new Dictionary<string, string>
            {
                ["value"] = "0",
                ["max"] = "100",
                ["showLabel"] = "true"
            }
        };

        // Assert
        Assert.Equal(3, widget.DefaultSettings.Count);
        Assert.Equal("0", widget.DefaultSettings["value"]);
        Assert.Equal("100", widget.DefaultSettings["max"]);
        Assert.Equal("true", widget.DefaultSettings["showLabel"]);
    }
}
