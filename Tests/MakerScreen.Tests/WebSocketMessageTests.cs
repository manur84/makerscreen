using Xunit;
using MakerScreen.Core.Models;
using FluentAssertions;

namespace MakerScreen.Tests;

public class WebSocketMessageTests
{
    [Fact]
    public void WebSocketMessage_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var message = new WebSocketMessage();

        // Assert
        message.Type.Should().BeEmpty();
        message.ClientId.Should().BeEmpty();
        message.Data.Should().BeNull();
        message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void WebSocketMessage_ShouldCreateRegisterMessage()
    {
        // Arrange & Act
        var message = new WebSocketMessage
        {
            Type = MessageTypes.Register,
            ClientId = "client-001",
            Data = new { name = "Test Client", macAddress = "00:11:22:33:44:55" }
        };

        // Assert
        message.Type.Should().Be("REGISTER");
        message.ClientId.Should().Be("client-001");
        message.Data.Should().NotBeNull();
    }

    [Fact]
    public void MessageTypes_ShouldContainAllTypes()
    {
        // Assert
        MessageTypes.Register.Should().Be("REGISTER");
        MessageTypes.Heartbeat.Should().Be("HEARTBEAT");
        MessageTypes.ContentUpdate.Should().Be("CONTENT_UPDATE");
        MessageTypes.Command.Should().Be("COMMAND");
        MessageTypes.InstallClient.Should().Be("INSTALL_CLIENT");
        MessageTypes.Status.Should().Be("STATUS");
        MessageTypes.Error.Should().Be("ERROR");
    }

    [Fact]
    public void WebSocketMessage_ShouldCreateContentUpdateMessage()
    {
        // Arrange
        var contentData = new
        {
            contentId = "content-001",
            name = "test-image.png",
            type = "image",
            mimeType = "image/png",
            data = "base64encodeddata"
        };

        // Act
        var message = new WebSocketMessage
        {
            Type = MessageTypes.ContentUpdate,
            Data = contentData
        };

        // Assert
        message.Type.Should().Be("CONTENT_UPDATE");
        message.Data.Should().NotBeNull();
    }

    [Fact]
    public void WebSocketMessage_ShouldCreateCommandMessage()
    {
        // Arrange & Act
        var message = new WebSocketMessage
        {
            Type = MessageTypes.Command,
            ClientId = "client-001",
            Data = new
            {
                command = "reboot",
                parameters = new Dictionary<string, string>
                {
                    ["delay"] = "10"
                }
            }
        };

        // Assert
        message.Type.Should().Be("COMMAND");
        message.ClientId.Should().Be("client-001");
    }
}
