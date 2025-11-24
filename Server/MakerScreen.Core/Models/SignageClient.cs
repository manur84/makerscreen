namespace MakerScreen.Core.Models;

/// <summary>
/// Represents a digital signage client device
/// </summary>
public class SignageClient
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public ClientStatus Status { get; set; }
    public DateTime LastSeen { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum ClientStatus
{
    Unknown,
    Online,
    Offline,
    Installing,
    Error
}
