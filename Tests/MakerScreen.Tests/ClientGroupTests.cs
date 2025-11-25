using Xunit;
using MakerScreen.Core.Models;
using FluentAssertions;

namespace MakerScreen.Tests;

public class ClientGroupTests
{
    [Fact]
    public void ClientGroup_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var group = new ClientGroup();

        // Assert
        group.Id.Should().NotBeNullOrEmpty();
        group.Name.Should().BeEmpty();
        group.Description.Should().BeEmpty();
        group.ClientIds.Should().NotBeNull();
        group.ClientIds.Should().BeEmpty();
        group.DefaultPlaylistId.Should().BeNull();
        group.Metadata.Should().NotBeNull();
        group.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ClientGroup_ShouldAddClients()
    {
        // Arrange
        var group = new ClientGroup
        {
            Name = "Lobby Displays"
        };

        // Act
        group.ClientIds.Add("client-1");
        group.ClientIds.Add("client-2");
        group.ClientIds.Add("client-3");

        // Assert
        group.ClientIds.Should().HaveCount(3);
        group.ClientIds.Should().Contain("client-1");
        group.ClientIds.Should().Contain("client-2");
        group.ClientIds.Should().Contain("client-3");
    }

    [Fact]
    public void ClientGroup_ShouldSupportMetadata()
    {
        // Arrange
        var group = new ClientGroup
        {
            Name = "Conference Room Displays",
            Description = "All displays in conference rooms"
        };

        // Act
        group.Metadata["location"] = "Building A";
        group.Metadata["floor"] = "2";
        group.Metadata["department"] = "Engineering";

        // Assert
        group.Metadata.Should().HaveCount(3);
        group.Metadata["location"].Should().Be("Building A");
        group.Metadata["floor"].Should().Be("2");
        group.Metadata["department"].Should().Be("Engineering");
    }

    [Fact]
    public void ClientGroup_ShouldAssignDefaultPlaylist()
    {
        // Arrange
        var playlistId = Guid.NewGuid().ToString();

        // Act
        var group = new ClientGroup
        {
            Name = "Reception Displays",
            DefaultPlaylistId = playlistId
        };

        // Assert
        group.DefaultPlaylistId.Should().Be(playlistId);
    }

    [Fact]
    public void ClientGroup_ShouldRemoveClients()
    {
        // Arrange
        var group = new ClientGroup
        {
            Name = "Test Group"
        };
        group.ClientIds.Add("client-1");
        group.ClientIds.Add("client-2");
        group.ClientIds.Add("client-3");

        // Act
        group.ClientIds.Remove("client-2");

        // Assert
        group.ClientIds.Should().HaveCount(2);
        group.ClientIds.Should().Contain("client-1");
        group.ClientIds.Should().NotContain("client-2");
        group.ClientIds.Should().Contain("client-3");
    }
}
