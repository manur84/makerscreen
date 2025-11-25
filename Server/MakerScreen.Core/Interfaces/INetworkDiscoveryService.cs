namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for network discovery of client devices
/// </summary>
public interface INetworkDiscoveryService
{
    Task StartDiscoveryAsync(CancellationToken cancellationToken = default);
    Task StopDiscoveryAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DiscoveredDevice>> DiscoverDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    event EventHandler<DiscoveredDevice>? DeviceDiscovered;
}

/// <summary>
/// Represents a discovered network device
/// </summary>
public class DiscoveredDevice
{
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Properties { get; set; } = new();
}
