namespace MakerScreen.Core.Models;

/// <summary>
/// Represents an emergency broadcast message that interrupts normal content
/// </summary>
public class EmergencyBroadcast
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public EmergencyPriority Priority { get; set; } = EmergencyPriority.High;
    public EmergencyType Type { get; set; } = EmergencyType.Alert;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = string.Empty;
    public List<string>? TargetGroupIds { get; set; }
    public List<string>? TargetClientIds { get; set; }
    public EmergencyStyle Style { get; set; } = new();
}

public enum EmergencyPriority
{
    Low = 0,
    Medium = 50,
    High = 100,
    Critical = 200
}

public enum EmergencyType
{
    Info,
    Alert,
    Warning,
    Emergency,
    Evacuation
}

/// <summary>
/// Visual styling for emergency broadcasts
/// </summary>
public class EmergencyStyle
{
    public string BackgroundColor { get; set; } = "#FF0000";
    public string TextColor { get; set; } = "#FFFFFF";
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 48;
    public bool ShowFlashing { get; set; } = true;
    public bool ShowFullScreen { get; set; } = true;
    public int DisplayDuration { get; set; } = 0; // 0 = indefinite until cleared
}
