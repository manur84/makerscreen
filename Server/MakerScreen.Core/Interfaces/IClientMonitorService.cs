using MakerScreen.Core.Models;

namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for monitoring client health and status
/// </summary>
public interface IClientMonitorService
{
    void RegisterClient(SignageClient client);
    void UnregisterClient(string clientId);
    void UpdateClientStatus(string clientId, ClientStatus status);
    void RecordHeartbeat(string clientId);
    IEnumerable<SignageClient> GetAllClients();
    SignageClient? GetClient(string clientId);
    IEnumerable<SignageClient> GetClientsByStatus(ClientStatus status);
    IEnumerable<SignageClient> GetStaleClients(TimeSpan timeout);
    event EventHandler<ClientStatusChangedEventArgs>? ClientStatusChanged;
    event EventHandler<string>? ClientDisconnected;
}

public class ClientStatusChangedEventArgs : EventArgs
{
    public string ClientId { get; }
    public ClientStatus OldStatus { get; }
    public ClientStatus NewStatus { get; }

    public ClientStatusChangedEventArgs(string clientId, ClientStatus oldStatus, ClientStatus newStatus)
    {
        ClientId = clientId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
