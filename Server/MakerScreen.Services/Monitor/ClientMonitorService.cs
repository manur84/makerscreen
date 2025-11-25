using System.Collections.Concurrent;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.Monitor;

/// <summary>
/// Monitors client health and status
/// </summary>
public class ClientMonitorService : IClientMonitorService
{
    private readonly ILogger<ClientMonitorService> _logger;
    private readonly ConcurrentDictionary<string, SignageClient> _clients = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastHeartbeats = new();
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(90);

    public event EventHandler<ClientStatusChangedEventArgs>? ClientStatusChanged;
    public event EventHandler<string>? ClientDisconnected;

    public ClientMonitorService(ILogger<ClientMonitorService> logger)
    {
        _logger = logger;
    }

    public void RegisterClient(SignageClient client)
    {
        _logger.LogInformation("Registering client: {Id} ({Name})", client.Id, client.Name);
        
        _clients[client.Id] = client;
        _lastHeartbeats[client.Id] = DateTime.UtcNow;
        
        OnClientStatusChanged(client.Id, ClientStatus.Unknown, ClientStatus.Online);
    }

    public void UnregisterClient(string clientId)
    {
        _logger.LogInformation("Unregistering client: {Id}", clientId);
        
        if (_clients.TryRemove(clientId, out var client))
        {
            _lastHeartbeats.TryRemove(clientId, out _);
            
            OnClientStatusChanged(clientId, client.Status, ClientStatus.Offline);
            ClientDisconnected?.Invoke(this, clientId);
        }
    }

    public void UpdateClientStatus(string clientId, ClientStatus status)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            var oldStatus = client.Status;
            client.Status = status;
            
            if (oldStatus != status)
            {
                _logger.LogInformation("Client {Id} status changed: {OldStatus} -> {NewStatus}", 
                    clientId, oldStatus, status);
                OnClientStatusChanged(clientId, oldStatus, status);
            }
        }
    }

    public void RecordHeartbeat(string clientId)
    {
        _lastHeartbeats[clientId] = DateTime.UtcNow;
        
        if (_clients.TryGetValue(clientId, out var client))
        {
            client.LastSeen = DateTime.UtcNow;
            
            if (client.Status != ClientStatus.Online)
            {
                UpdateClientStatus(clientId, ClientStatus.Online);
            }
        }
    }

    public IEnumerable<SignageClient> GetAllClients()
    {
        return _clients.Values.ToList();
    }

    public SignageClient? GetClient(string clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    public IEnumerable<SignageClient> GetClientsByStatus(ClientStatus status)
    {
        return _clients.Values.Where(c => c.Status == status).ToList();
    }

    public IEnumerable<SignageClient> GetStaleClients(TimeSpan timeout)
    {
        var staleThreshold = DateTime.UtcNow - timeout;
        
        return _clients.Values
            .Where(c => c.LastSeen < staleThreshold)
            .ToList();
    }

    public void CheckClientHealth()
    {
        var staleClients = GetStaleClients(_defaultTimeout);
        
        foreach (var client in staleClients)
        {
            if (client.Status == ClientStatus.Online)
            {
                _logger.LogWarning("Client {Id} ({Name}) has gone stale", client.Id, client.Name);
                UpdateClientStatus(client.Id, ClientStatus.Offline);
            }
        }
    }

    private void OnClientStatusChanged(string clientId, ClientStatus oldStatus, ClientStatus newStatus)
    {
        ClientStatusChanged?.Invoke(this, new ClientStatusChangedEventArgs(clientId, oldStatus, newStatus));
    }
}
