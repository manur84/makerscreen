using Xunit;
using MakerScreen.Core.Models;
using FluentAssertions;

namespace MakerScreen.Tests;

public class ContentItemTests
{
    [Fact]
    public void ContentItem_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var content = new ContentItem();

        // Assert
        content.Id.Should().NotBeNullOrEmpty();
        content.Name.Should().BeEmpty();
        content.Duration.Should().Be(10);
        content.Data.Should().NotBeNull();
        content.Data.Should().BeEmpty();
        content.MimeType.Should().BeEmpty();
        content.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void ContentItem_ShouldAcceptImageData()
    {
        // Arrange
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var content = new ContentItem
        {
            Name = "test-image.png",
            Type = ContentType.Image,
            MimeType = "image/png",
            Data = imageData,
            Duration = 15
        };

        // Assert
        content.Type.Should().Be(ContentType.Image);
        content.MimeType.Should().Be("image/png");
        content.Data.Should().BeEquivalentTo(imageData);
        content.Duration.Should().Be(15);
    }

    [Fact]
    public void ContentItem_ShouldSupportAllTypes()
    {
        // Arrange & Act
        var types = Enum.GetValues<ContentType>();

        // Assert
        types.Should().Contain(ContentType.Image);
        types.Should().Contain(ContentType.Video);
        types.Should().Contain(ContentType.Html);
        types.Should().Contain(ContentType.Url);
    }

    [Fact]
    public void ContentItem_ShouldSupportMetadata()
    {
        // Arrange
        var content = new ContentItem
        {
            Name = "Weather Widget",
            Type = ContentType.Html
        };

        // Act
        content.Metadata["location"] = "New York";
        content.Metadata["refreshInterval"] = "300";

        // Assert
        content.Metadata.Should().HaveCount(2);
        content.Metadata["location"].Should().Be("New York");
    }
}
