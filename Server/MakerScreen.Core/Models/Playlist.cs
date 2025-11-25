namespace MakerScreen.Core.Models;

/// <summary>
/// Represents a playlist of content items with scheduling
/// </summary>
public class Playlist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PlaylistItem> Items { get; set; } = new();
    public PlaylistSchedule? Schedule { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a single item in a playlist
/// </summary>
public class PlaylistItem
{
    public int Order { get; set; }
    public string ContentId { get; set; } = string.Empty;
    public int Duration { get; set; } = 10; // seconds
    public TransitionType Transition { get; set; } = TransitionType.None;
    public int TransitionDuration { get; set; } = 500; // milliseconds
}

/// <summary>
/// Defines when a playlist should be active
/// </summary>
public class PlaylistSchedule
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public List<DayOfWeek> ActiveDays { get; set; } = new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };
    public SchedulePriority Priority { get; set; } = SchedulePriority.Normal;
}

public enum TransitionType
{
    None,
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    Zoom
}

public enum SchedulePriority
{
    Low = 0,
    Normal = 50,
    High = 100,
    Emergency = 1000
}
