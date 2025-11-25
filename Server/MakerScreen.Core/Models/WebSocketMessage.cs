namespace MakerScreen.Core.Models;

/// <summary>
/// WebSocket message protocol
/// </summary>
public class WebSocketMessage
{
    public string Type { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class MessageTypes
{
    public const string Register = "REGISTER";
    public const string Heartbeat = "HEARTBEAT";
    public const string ContentUpdate = "CONTENT_UPDATE";
    public const string Command = "COMMAND";
    public const string InstallClient = "INSTALL_CLIENT";
    public const string Status = "STATUS";
    public const string Error = "ERROR";
    public const string PlaylistUpdate = "PLAYLIST_UPDATE";
    public const string OverlayUpdate = "OVERLAY_UPDATE";
    public const string EmergencyBroadcast = "EMERGENCY_BROADCAST";
    public const string EmergencyClear = "EMERGENCY_CLEAR";
}
