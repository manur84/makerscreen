using Xunit;
using MakerScreen.Core.Models;
using FluentAssertions;

namespace MakerScreen.Tests;

public class PlaylistTests
{
    [Fact]
    public void Playlist_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var playlist = new Playlist();

        // Assert
        playlist.Id.Should().NotBeNullOrEmpty();
        playlist.Name.Should().BeEmpty();
        playlist.Items.Should().NotBeNull();
        playlist.Items.Should().BeEmpty();
        playlist.IsActive.Should().BeTrue();
        playlist.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Playlist_ShouldAcceptItems()
    {
        // Arrange
        var playlist = new Playlist
        {
            Name = "Test Playlist"
        };

        var item = new PlaylistItem
        {
            Order = 1,
            ContentId = "content-123",
            Duration = 15,
            Transition = TransitionType.Fade,
            TransitionDuration = 500
        };

        // Act
        playlist.Items.Add(item);

        // Assert
        playlist.Items.Should().HaveCount(1);
        playlist.Items[0].ContentId.Should().Be("content-123");
        playlist.Items[0].Duration.Should().Be(15);
    }

    [Fact]
    public void PlaylistSchedule_ShouldDefaultToAllDays()
    {
        // Arrange & Act
        var schedule = new PlaylistSchedule();

        // Assert
        schedule.ActiveDays.Should().HaveCount(7);
        schedule.ActiveDays.Should().Contain(DayOfWeek.Monday);
        schedule.ActiveDays.Should().Contain(DayOfWeek.Sunday);
        schedule.Priority.Should().Be(SchedulePriority.Normal);
    }

    [Fact]
    public void PlaylistSchedule_ShouldAcceptTimeRange()
    {
        // Arrange
        var schedule = new PlaylistSchedule
        {
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            ActiveDays = new List<DayOfWeek> 
            { 
                DayOfWeek.Monday, 
                DayOfWeek.Tuesday, 
                DayOfWeek.Wednesday 
            }
        };

        // Assert
        schedule.StartTime.Should().Be(new TimeSpan(9, 0, 0));
        schedule.EndTime.Should().Be(new TimeSpan(17, 0, 0));
        schedule.ActiveDays.Should().HaveCount(3);
    }
}
