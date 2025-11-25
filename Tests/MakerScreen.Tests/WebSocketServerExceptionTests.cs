using Xunit;
using FluentAssertions;
using MakerScreen.Services.WebSocket;

namespace MakerScreen.Tests;

public class WebSocketServerExceptionTests
{
    [Fact]
    public void WebSocketServerException_ShouldCreateWithMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new WebSocketServerException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void WebSocketServerException_ShouldCreateWithMessageAndInnerException()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new WebSocketServerException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void WebSocketServerException_ShouldContainAccessDeniedGuidance()
    {
        // Arrange
        var expectedGuidance = "Administrator";
        var message = $"Failed to start WebSocket server. Please run the application as {expectedGuidance}.";

        // Act
        var exception = new WebSocketServerException(message);

        // Assert
        exception.Message.Should().Contain(expectedGuidance);
    }

    [Fact]
    public void WebSocketServerException_IsException()
    {
        // Act
        var exception = new WebSocketServerException("Test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void WebSocketServerException_CanBeThrown()
    {
        // Arrange
        var message = "Server failed to start";

        // Act & Assert
        Action act = () => throw new WebSocketServerException(message);
        act.Should().Throw<WebSocketServerException>()
            .WithMessage(message);
    }
}
