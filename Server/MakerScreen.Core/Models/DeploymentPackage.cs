namespace MakerScreen.Core.Models;

/// <summary>
/// Client deployment package information
/// </summary>
public class DeploymentPackage
{
    public string Version { get; set; } = string.Empty;
    public byte[] PackageData { get; set; } = Array.Empty<byte>();
    public string Hash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Configuration { get; set; } = new();
}
