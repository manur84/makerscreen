using Xunit;
using MakerScreen.Core.Models;
using FluentAssertions;

namespace MakerScreen.Tests;

public class OverlayTests
{
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
}
