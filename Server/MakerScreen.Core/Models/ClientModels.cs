namespace MakerScreen.Core.Models;

public class Client
{
    public int ClientID { get; set; }
    public Guid ClientGUID { get; set; } = Guid.NewGuid();
    public string Hostname { get; set; } = string.Empty;
    public string MACAddress { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string? Model { get; set; }
    public string Status { get; set; } = "Offline";
    public DateTime? LastHeartbeat { get; set; }
    public bool Approved { get; set; }
    public DateTime Registered { get; set; } = DateTime.UtcNow;
}
