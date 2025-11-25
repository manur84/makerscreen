using Xunit;
using MakerScreen.Core.Models;
using FluentAssertions;

namespace MakerScreen.Tests;

public class SignageClientTests
{
    [Fact]
    public void SignageClient_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var client = new SignageClient();

        // Assert
        client.Id.Should().BeEmpty();
        client.Name.Should().BeEmpty();
        client.IpAddress.Should().BeEmpty();
        client.MacAddress.Should().BeEmpty();
        client.Version.Should().BeEmpty();
        client.Status.Should().Be(ClientStatus.Unknown);
        client.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void SignageClient_ShouldTrackStatus()
    {
        // Arrange
        var client = new SignageClient
        {
            Id = "client-001",
            Name = "Living Room Display",
            IpAddress = "192.168.1.100",
            MacAddress = "B8:27:EB:12:34:56",
            Version = "1.0.0"
        };

        // Act
        client.Status = ClientStatus.Online;
        client.LastSeen = DateTime.UtcNow;

        // Assert
        client.Status.Should().Be(ClientStatus.Online);
        client.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ClientStatus_ShouldHaveAllStates()
    {
        // Arrange & Act
        var states = Enum.GetValues<ClientStatus>();

        // Assert
        states.Should().Contain(ClientStatus.Unknown);
        states.Should().Contain(ClientStatus.Online);
        states.Should().Contain(ClientStatus.Offline);
        states.Should().Contain(ClientStatus.Installing);
        states.Should().Contain(ClientStatus.Error);
    }

    [Fact]
    public void SignageClient_ShouldSupportMetadata()
    {
        // Arrange
        var client = new SignageClient
        {
            Id = "client-002",
            Name = "Conference Room"
        };

        // Act
        client.Metadata["location"] = "Building A, Floor 2";
        client.Metadata["screenResolution"] = "1920x1080";
        client.Metadata["lastReboot"] = DateTime.UtcNow.ToString("o");

        // Assert
        client.Metadata.Should().HaveCount(3);
        client.Metadata["location"].Should().Contain("Building A");
    }
}
