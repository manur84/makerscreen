namespace MakerScreen.Core.Models;

/// <summary>
/// Server configuration settings
/// </summary>
public class ServerSettings
{
    public int WebSocketPort { get; set; } = 8443;
    public int ApiPort { get; set; } = 5000;
    public string ContentPath { get; set; } = "./Content";
    public string DeploymentsPath { get; set; } = "./Deployments";
    public bool EnableSsl { get; set; } = false;
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public int HeartbeatTimeout { get; set; } = 90; // seconds
    public int ClientRefreshInterval { get; set; } = 30; // seconds
    public bool EnableAutoDiscovery { get; set; } = true;
    public string? DatabaseConnectionString { get; set; }
    public LogSettings Logging { get; set; } = new();
}

/// <summary>
/// Logging configuration
/// </summary>
public class LogSettings
{
    public string Level { get; set; } = "Information";
    public string? FilePath { get; set; }
    public bool EnableConsole { get; set; } = true;
    public int MaxFileSizeMb { get; set; } = 10;
    public int MaxFileCount { get; set; } = 5;
}
