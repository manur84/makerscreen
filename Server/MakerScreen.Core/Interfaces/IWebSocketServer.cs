using MakerScreen.Core.Models;

namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for managing WebSocket server connections
/// </summary>
public interface IWebSocketServer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(string clientId, WebSocketMessage message, CancellationToken cancellationToken = default);
    Task BroadcastMessageAsync(WebSocketMessage message, CancellationToken cancellationToken = default);
    IReadOnlyCollection<SignageClient> GetConnectedClients();
}
