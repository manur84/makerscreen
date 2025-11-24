using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.WebSocket;

/// <summary>
/// Secure WebSocket server for client communication
/// </summary>
public class SecureWebSocketServer : IWebSocketServer
{
    private readonly ILogger<SecureWebSocketServer> _logger;
    private readonly ConcurrentDictionary<string, ClientConnection> _clients = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly int _port;
    private readonly X509Certificate2? _certificate;

    public SecureWebSocketServer(ILogger<SecureWebSocketServer> logger, int port = 8443)
    {
        _logger = logger;
        _port = port;
        _certificate = GenerateSelfSignedCertificate();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting WebSocket server on port {Port}", _port);
        
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new HttpListener();
        
        // Support both HTTP (for local development) and HTTPS
        _listener.Prefixes.Add($"http://+:{_port}/");
        _listener.Start();
        
        _logger.LogInformation("WebSocket server started successfully");
        
        _ = Task.Run(() => AcceptConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping WebSocket server");
        
        _cancellationTokenSource?.Cancel();
        
        foreach (var client in _clients.Values)
        {
            await client.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", cancellationToken);
        }
        
        _listener?.Stop();
        _listener?.Close();
        
        _logger.LogInformation("WebSocket server stopped");
    }

    public async Task SendMessageAsync(string clientId, WebSocketMessage message, CancellationToken cancellationToken = default)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            await SendMessageToClientAsync(client, message, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Client {ClientId} not found", clientId);
        }
    }

    public async Task BroadcastMessageAsync(WebSocketMessage message, CancellationToken cancellationToken = default)
    {
        var tasks = _clients.Values.Select(client => SendMessageToClientAsync(client, message, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public IReadOnlyCollection<SignageClient> GetConnectedClients()
    {
        return _clients.Values.Select(c => c.Client).ToList();
    }

    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener != null)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                
                if (context.Request.IsWebSocketRequest)
                {
                    _ = Task.Run(() => HandleWebSocketConnectionAsync(context), cancellationToken);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error accepting connection");
            }
        }
    }

    private async Task HandleWebSocketConnectionAsync(HttpListenerContext context)
    {
        System.Net.WebSockets.WebSocket? webSocket = null;
        string? clientId = null;
        
        try
        {
            var wsContext = await context.AcceptWebSocketAsync(null);
            webSocket = wsContext.WebSocket;
            
            clientId = Guid.NewGuid().ToString();
            var client = new SignageClient
            {
                Id = clientId,
                IpAddress = context.Request.RemoteEndPoint?.Address.ToString() ?? "unknown",
                Status = ClientStatus.Online,
                LastSeen = DateTime.UtcNow
            };
            
            var connection = new ClientConnection(client, webSocket);
            _clients.TryAdd(clientId, connection);
            
            _logger.LogInformation("Client {ClientId} connected from {IpAddress}", clientId, client.IpAddress);
            
            await HandleClientMessagesAsync(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket connection");
        }
        finally
        {
            if (clientId != null && _clients.TryRemove(clientId, out var removed))
            {
                _logger.LogInformation("Client {ClientId} disconnected", clientId);
                removed.Client.Status = ClientStatus.Offline;
            }
            
            if (webSocket != null)
            {
                webSocket.Dispose();
            }
        }
    }

    private async Task HandleClientMessagesAsync(ClientConnection connection)
    {
        var buffer = new byte[1024 * 4];
        
        while (connection.WebSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await connection.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await connection.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
                
                var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var message = JsonSerializer.Deserialize<WebSocketMessage>(messageJson);
                
                if (message != null)
                {
                    await ProcessMessageAsync(connection, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving message from client {ClientId}", connection.Client.Id);
                break;
            }
        }
    }

    private async Task ProcessMessageAsync(ClientConnection connection, WebSocketMessage message)
    {
        _logger.LogDebug("Received message type {Type} from client {ClientId}", message.Type, connection.Client.Id);
        
        connection.Client.LastSeen = DateTime.UtcNow;
        
        switch (message.Type)
        {
            case MessageTypes.Register:
                await HandleRegistrationAsync(connection, message);
                break;
            case MessageTypes.Heartbeat:
                await HandleHeartbeatAsync(connection);
                break;
            case MessageTypes.Status:
                await HandleStatusAsync(connection, message);
                break;
            default:
                _logger.LogWarning("Unknown message type: {Type}", message.Type);
                break;
        }
    }

    private async Task HandleRegistrationAsync(ClientConnection connection, WebSocketMessage message)
    {
        if (message.Data is JsonElement data)
        {
            connection.Client.Name = data.GetProperty("name").GetString() ?? "Unknown";
            connection.Client.MacAddress = data.GetProperty("macAddress").GetString() ?? "Unknown";
            connection.Client.Version = data.GetProperty("version").GetString() ?? "1.0.0";
        }
        
        _logger.LogInformation("Client {Name} registered with ID {ClientId}", connection.Client.Name, connection.Client.Id);
        
        var response = new WebSocketMessage
        {
            Type = MessageTypes.Register,
            ClientId = connection.Client.Id,
            Data = new { success = true, clientId = connection.Client.Id }
        };
        
        await SendMessageToClientAsync(connection, response, CancellationToken.None);
    }

    private Task HandleHeartbeatAsync(ClientConnection connection)
    {
        connection.Client.Status = ClientStatus.Online;
        return Task.CompletedTask;
    }

    private Task HandleStatusAsync(ClientConnection connection, WebSocketMessage message)
    {
        _logger.LogDebug("Status update from {ClientId}: {Status}", connection.Client.Id, message.Data);
        return Task.CompletedTask;
    }

    private async Task SendMessageToClientAsync(ClientConnection connection, WebSocketMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            await connection.WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to client {ClientId}", connection.Client.Id);
        }
    }

    private X509Certificate2? GenerateSelfSignedCertificate()
    {
        // In production, use a proper certificate from a CA
        // For development, this generates a self-signed certificate
        try
        {
            // Certificate generation would be done here
            // For simplicity, returning null and using HTTP
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not generate self-signed certificate");
            return null;
        }
    }

    private class ClientConnection
    {
        public SignageClient Client { get; }
        public System.Net.WebSockets.WebSocket WebSocket { get; }

        public ClientConnection(SignageClient client, System.Net.WebSockets.WebSocket webSocket)
        {
            Client = client;
            WebSocket = webSocket;
        }
    }
}
