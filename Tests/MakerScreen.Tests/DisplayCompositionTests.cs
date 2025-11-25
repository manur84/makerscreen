using Xunit;
using MakerScreen.Core.Models;

namespace MakerScreen.Tests;

public class DisplayCompositionTests
{
    [Fact]
    public void DisplayComposition_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var composition = new DisplayComposition();

        // Assert
        Assert.NotNull(composition.Id);
        Assert.NotEmpty(composition.Id);
        Assert.Equal(string.Empty, composition.Name);
        Assert.NotNull(composition.Resolution);
        Assert.NotNull(composition.Background);
        Assert.NotNull(composition.Overlays);
        Assert.Empty(composition.Overlays);
        Assert.True(composition.IsActive);
    }

    [Fact]
    public void CompositionResolution_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var resolution = new CompositionResolution();

        // Assert
        Assert.Equal(1920, resolution.Width);
        Assert.Equal(1080, resolution.Height);
        Assert.Equal("Full HD (1920x1080)", resolution.PresetName);
    }

    [Fact]
    public void CompositionResolution_ShouldCalculateAspectRatio()
    {
        // Arrange
        var resolution = new CompositionResolution
        {
            Width = 1920,
            Height = 1080
        };

        // Act
        var aspectRatio = resolution.AspectRatio;

        // Assert
        Assert.Equal("16:9", aspectRatio);
    }

    [Fact]
    public void CompositionResolution_ShouldCalculatePortraitAspectRatio()
    {
        // Arrange
        var resolution = new CompositionResolution
        {
            Width = 1080,
            Height = 1920
        };

        // Act
        var aspectRatio = resolution.AspectRatio;

        // Assert
        Assert.Equal("9:16", aspectRatio);
    }

    [Fact]
    public void CompositionBackground_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var background = new CompositionBackground();

        // Assert
        Assert.Equal(BackgroundType.Color, background.Type);
        Assert.Equal("#000000", background.Color);
        Assert.Null(background.ImageContentId);
        Assert.Null(background.ImageData);
        Assert.Equal(ImageScaleMode.Fill, background.ScaleMode);
    }

    [Fact]
    public void CompositionBackground_ShouldSupportImageType()
    {
        // Arrange
        var background = new CompositionBackground
        {
            Type = BackgroundType.Image,
            ImageContentId = "image-123",
            ImageData = new byte[] { 1, 2, 3, 4 },
            ScaleMode = ImageScaleMode.Fit
        };

        // Assert
        Assert.Equal(BackgroundType.Image, background.Type);
        Assert.Equal("image-123", background.ImageContentId);
        Assert.NotNull(background.ImageData);
        Assert.Equal(4, background.ImageData.Length);
        Assert.Equal(ImageScaleMode.Fit, background.ScaleMode);
    }

    [Fact]
    public void CompositionOverlay_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var overlay = new CompositionOverlay();

        // Assert
        Assert.NotNull(overlay.Id);
        Assert.NotEmpty(overlay.Id);
        Assert.Equal(string.Empty, overlay.OverlayId);
        Assert.Equal(string.Empty, overlay.Name);
        Assert.Equal(0, overlay.X);
        Assert.Equal(0, overlay.Y);
        Assert.Equal(200, overlay.Width);
        Assert.Equal(50, overlay.Height);
        Assert.Equal(0, overlay.ZIndex);
        Assert.True(overlay.IsVisible);
        Assert.False(overlay.IsLocked);
    }

    [Fact]
    public void CompositionOverlay_ShouldSupportPositioning()
    {
        // Arrange
        var overlay = new CompositionOverlay
        {
            X = 100,
            Y = 200,
            Width = 300,
            Height = 150,
            ZIndex = 5
        };

        // Assert
        Assert.Equal(100, overlay.X);
        Assert.Equal(200, overlay.Y);
        Assert.Equal(300, overlay.Width);
        Assert.Equal(150, overlay.Height);
        Assert.Equal(5, overlay.ZIndex);
    }

    [Fact]
    public void CompositionOverlay_ShouldSupportVisibilityAndLocking()
    {
        // Arrange
        var overlay = new CompositionOverlay
        {
            IsVisible = false,
            IsLocked = true
        };

        // Assert
        Assert.False(overlay.IsVisible);
        Assert.True(overlay.IsLocked);
    }

    [Fact]
    public void DisplayComposition_ShouldAddOverlays()
    {
        // Arrange
        var composition = new DisplayComposition
        {
            Name = "Test Scene"
        };

        var overlay1 = new CompositionOverlay { Name = "Overlay 1", ZIndex = 0 };
        var overlay2 = new CompositionOverlay { Name = "Overlay 2", ZIndex = 1 };

        // Act
        composition.Overlays.Add(overlay1);
        composition.Overlays.Add(overlay2);

        // Assert
        Assert.Equal(2, composition.Overlays.Count);
        Assert.Equal("Overlay 1", composition.Overlays[0].Name);
        Assert.Equal("Overlay 2", composition.Overlays[1].Name);
    }

    [Fact]
    public void ResolutionPresets_ShouldContainCommonPresets()
    {
        // Assert
        Assert.True(ResolutionPresets.Presets.ContainsKey("Full HD (1920x1080)"));
        Assert.True(ResolutionPresets.Presets.ContainsKey("4K (3840x2160)"));
        Assert.True(ResolutionPresets.Presets.ContainsKey("HD (1280x720)"));
        Assert.True(ResolutionPresets.Presets.ContainsKey("Portrait Full HD (1080x1920)"));
    }

    [Fact]
    public void ResolutionPresets_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var fullHd = ResolutionPresets.Presets["Full HD (1920x1080)"];
        var hd = ResolutionPresets.Presets["HD (1280x720)"];
        var fourK = ResolutionPresets.Presets["4K (3840x2160)"];

        // Assert
        Assert.Equal((1920, 1080), fullHd);
        Assert.Equal((1280, 720), hd);
        Assert.Equal((3840, 2160), fourK);
    }

    [Fact]
    public void BackgroundType_ShouldHaveAllTypes()
    {
        // Assert
        Assert.Equal(3, Enum.GetValues<BackgroundType>().Length);
        Assert.True(Enum.IsDefined(typeof(BackgroundType), BackgroundType.None));
        Assert.True(Enum.IsDefined(typeof(BackgroundType), BackgroundType.Color));
        Assert.True(Enum.IsDefined(typeof(BackgroundType), BackgroundType.Image));
    }

    [Fact]
    public void ImageScaleMode_ShouldHaveAllModes()
    {
        // Assert
        Assert.Equal(5, Enum.GetValues<ImageScaleMode>().Length);
        Assert.True(Enum.IsDefined(typeof(ImageScaleMode), ImageScaleMode.Fill));
        Assert.True(Enum.IsDefined(typeof(ImageScaleMode), ImageScaleMode.Fit));
        Assert.True(Enum.IsDefined(typeof(ImageScaleMode), ImageScaleMode.Stretch));
        Assert.True(Enum.IsDefined(typeof(ImageScaleMode), ImageScaleMode.Center));
        Assert.True(Enum.IsDefined(typeof(ImageScaleMode), ImageScaleMode.Tile));
    }

    [Fact]
    public void DisplayComposition_ShouldTrackModificationTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var composition = new DisplayComposition();
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(composition.CreatedAt >= beforeCreation && composition.CreatedAt <= afterCreation);
        Assert.True(composition.ModifiedAt >= beforeCreation && composition.ModifiedAt <= afterCreation);
    }
}
