using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MakerScreen.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.Discovery;

/// <summary>
/// Discovers devices on the local network
/// </summary>
public class NetworkDiscoveryService : INetworkDiscoveryService
{
    private readonly ILogger<NetworkDiscoveryService> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning;

    // Raspberry Pi MAC address prefixes
    private static readonly string[] RaspberryPiMacPrefixes = new[]
    {
        "B8:27:EB",  // Raspberry Pi Foundation
        "DC:A6:32",  // Raspberry Pi Foundation
        "E4:5F:01",  // Raspberry Pi Foundation
        "D8:3A:DD",  // Raspberry Pi Foundation
        "28:CD:C1"   // Raspberry Pi Foundation
    };

    public event EventHandler<DiscoveredDevice>? DeviceDiscovered;

    public NetworkDiscoveryService(ILogger<NetworkDiscoveryService> logger)
    {
        _logger = logger;
    }

    public async Task StartDiscoveryAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        _logger.LogInformation("Starting network discovery service");
        _isRunning = true;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _ = Task.Run(() => ContinuousDiscoveryAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public Task StopDiscoveryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping network discovery service");
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<DiscoveredDevice>> DiscoverDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering devices on local network...");
        
        var devices = new List<DiscoveredDevice>();
        var localIp = GetLocalIpAddress();
        
        if (localIp == null)
        {
            _logger.LogWarning("Could not determine local IP address");
            return devices;
        }

        var subnet = GetSubnet(localIp);
        _logger.LogInformation("Scanning subnet: {Subnet}.0/24", subnet);

        var tasks = new List<Task<DiscoveredDevice?>>();
        
        for (int i = 1; i < 255; i++)
        {
            var ip = $"{subnet}.{i}";
            tasks.Add(ProbeHostAsync(ip, timeout, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        
        foreach (var device in results)
        {
            if (device != null)
            {
                devices.Add(device);
                _logger.LogInformation("Discovered device: {Hostname} ({Ip})", device.Hostname, device.IpAddress);
            }
        }

        return devices;
    }

    private async Task ContinuousDiscoveryAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            try
            {
                var devices = await DiscoverDevicesAsync(TimeSpan.FromSeconds(5), cancellationToken);
                
                foreach (var device in devices)
                {
                    DeviceDiscovered?.Invoke(this, device);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during network discovery");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }

    private async Task<DiscoveredDevice?> ProbeHostAsync(string ip, TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip, (int)timeout.TotalMilliseconds);
            
            if (reply.Status == IPStatus.Success)
            {
                var hostname = await GetHostnameAsync(ip);
                var macAddress = await GetMacAddressAsync(ip);
                var deviceType = DetermineDeviceType(macAddress);

                return new DiscoveredDevice
                {
                    IpAddress = ip,
                    MacAddress = macAddress,
                    Hostname = hostname,
                    DeviceType = deviceType,
                    DiscoveredAt = DateTime.UtcNow,
                    Properties = new Dictionary<string, string>
                    {
                        ["pingTime"] = reply.RoundtripTime.ToString()
                    }
                };
            }
        }
        catch (Exception)
        {
            // Host not reachable
        }

        return null;
    }

    private async Task<string> GetHostnameAsync(string ip)
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(ip);
            return hostEntry.HostName;
        }
        catch
        {
            return ip;
        }
    }

    private async Task<string> GetMacAddressAsync(string ip)
    {
        // On Windows, use ARP
        // On Linux, use /proc/net/arp or ip neigh
        try
        {
            var arpFile = "/proc/net/arp";
            if (File.Exists(arpFile))
            {
                var lines = await File.ReadAllLinesAsync(arpFile);
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4 && parts[0] == ip)
                    {
                        return parts[3].ToUpper().Replace(":", ":");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get MAC address for {Ip}", ip);
        }

        return "Unknown";
    }

    private string DetermineDeviceType(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress) || macAddress == "Unknown")
            return "Unknown";

        var normalizedMac = macAddress.ToUpper().Replace("-", ":");
        var prefix = string.Join(":", normalizedMac.Split(':').Take(3));

        if (RaspberryPiMacPrefixes.Contains(prefix))
            return "Raspberry Pi";

        return "Unknown";
    }

    private IPAddress? GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            var endpoint = socket.LocalEndPoint as IPEndPoint;
            return endpoint?.Address;
        }
        catch
        {
            return null;
        }
    }

    private string GetSubnet(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return $"{bytes[0]}.{bytes[1]}.{bytes[2]}";
    }
}
