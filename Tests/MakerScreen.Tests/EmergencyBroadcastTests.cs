using Xunit;
using MakerScreen.Core.Models;
using FluentAssertions;

namespace MakerScreen.Tests;

public class EmergencyBroadcastTests
{
    [Fact]
    public void EmergencyBroadcast_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var broadcast = new EmergencyBroadcast();

        // Assert
        broadcast.Id.Should().NotBeNullOrEmpty();
        broadcast.Title.Should().BeEmpty();
        broadcast.Message.Should().BeEmpty();
        broadcast.Priority.Should().Be(EmergencyPriority.High);
        broadcast.Type.Should().Be(EmergencyType.Alert);
        broadcast.IsActive.Should().BeTrue();
        broadcast.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        broadcast.Style.Should().NotBeNull();
    }

    [Fact]
    public void EmergencyBroadcast_ShouldSetPriority()
    {
        // Arrange & Act
        var broadcast = new EmergencyBroadcast
        {
            Title = "Critical Alert",
            Message = "This is a critical emergency",
            Priority = EmergencyPriority.Critical,
            Type = EmergencyType.Evacuation
        };

        // Assert
        broadcast.Priority.Should().Be(EmergencyPriority.Critical);
        broadcast.Type.Should().Be(EmergencyType.Evacuation);
    }

    [Fact]
    public void EmergencyStyle_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var style = new EmergencyStyle();

        // Assert
        style.BackgroundColor.Should().Be("#FF0000");
        style.TextColor.Should().Be("#FFFFFF");
        style.FontFamily.Should().Be("Arial");
        style.FontSize.Should().Be(48);
        style.ShowFlashing.Should().BeTrue();
        style.ShowFullScreen.Should().BeTrue();
        style.DisplayDuration.Should().Be(0);
    }

    [Fact]
    public void EmergencyBroadcast_ShouldSupportTargeting()
    {
        // Arrange & Act
        var broadcast = new EmergencyBroadcast
        {
            Title = "Targeted Alert",
            Message = "This goes to specific clients",
            TargetClientIds = new List<string> { "client1", "client2" },
            TargetGroupIds = new List<string> { "group1" }
        };

        // Assert
        broadcast.TargetClientIds.Should().HaveCount(2);
        broadcast.TargetGroupIds.Should().HaveCount(1);
        broadcast.TargetClientIds.Should().Contain("client1");
        broadcast.TargetGroupIds.Should().Contain("group1");
    }

    [Fact]
    public void EmergencyBroadcast_ShouldSupportExpiration()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        
        // Act
        var broadcast = new EmergencyBroadcast
        {
            Title = "Expiring Alert",
            Message = "This expires soon",
            ExpiresAt = expiresAt
        };

        // Assert
        broadcast.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void EmergencyPriority_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)EmergencyPriority.Low).Should().Be(0);
        ((int)EmergencyPriority.Medium).Should().Be(50);
        ((int)EmergencyPriority.High).Should().Be(100);
        ((int)EmergencyPriority.Critical).Should().Be(200);
    }

    [Fact]
    public void EmergencyType_ShouldHaveAllTypes()
    {
        // Arrange & Act
        var types = Enum.GetValues<EmergencyType>();

        // Assert
        types.Should().Contain(EmergencyType.Info);
        types.Should().Contain(EmergencyType.Alert);
        types.Should().Contain(EmergencyType.Warning);
        types.Should().Contain(EmergencyType.Emergency);
        types.Should().Contain(EmergencyType.Evacuation);
    }
}
