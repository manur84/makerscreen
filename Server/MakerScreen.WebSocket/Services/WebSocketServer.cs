using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MakerScreen.WebSocket.Services;

public class WebSocketMessage
{
    public string Type { get; set; } = string.Empty;
    public JsonElement Data { get; set; }
}

public class WebSocketServer
{
    private readonly ILogger<WebSocketServer> _logger;
    private readonly Dictionary<string, System.Net.WebSockets.WebSocket> _clients = new();
    private HttpListener? _listener;

    public WebSocketServer(ILogger<WebSocketServer> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(string prefix = "http://localhost:8080/ws/")
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _listener.Start();
        _logger.LogInformation("WebSocket server started on {Prefix}", prefix);

        while (true)
        {
            var context = await _listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                _ = Task.Run(async () => await HandleWebSocketAsync(context));
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private async Task HandleWebSocketAsync(HttpListenerContext context)
    {
        WebSocketContext? wsContext = null;
        try
        {
            wsContext = await context.AcceptWebSocketAsync(null);
            var ws = wsContext.WebSocket;
            var clientId = Guid.NewGuid().ToString();
            _clients[clientId] = ws;

            _logger.LogInformation("Client connected: {ClientId}", clientId);

            var buffer = new byte[1024 * 4];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                    _clients.Remove(clientId);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await HandleMessageAsync(clientId, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket error");
        }
    }

    private async Task HandleMessageAsync(string clientId, string message)
    {
        try
        {
            var wsMessage = JsonSerializer.Deserialize<WebSocketMessage>(message);
            if (wsMessage == null) return;

            _logger.LogInformation("Received {Type} from {ClientId}", wsMessage.Type, clientId);

            switch (wsMessage.Type)
            {
                case "register":
                    await HandleRegisterAsync(clientId, wsMessage.Data);
                    break;
                case "heartbeat":
                    await HandleHeartbeatAsync(clientId);
                    break;
                default:
                    _logger.LogWarning("Unknown message type: {Type}", wsMessage.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message");
        }
    }

    private async Task HandleRegisterAsync(string clientId, JsonElement data)
    {
        var response = new
        {
            type = "registered",
            data = new { clientId, status = "approved" }
        };

        await SendMessageAsync(clientId, response);
    }

    private async Task HandleHeartbeatAsync(string clientId)
    {
        var response = new
        {
            type = "heartbeat_ack",
            data = new { timestamp = DateTime.UtcNow }
        };

        await SendMessageAsync(clientId, response);
    }

    public async Task SendMessageAsync(string clientId, object message)
    {
        if (!_clients.TryGetValue(clientId, out var ws) || ws.State != WebSocketState.Open)
            return;

        var json = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task BroadcastAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(json);

        foreach (var client in _clients.Values)
        {
            if (client.State == WebSocketState.Open)
            {
                await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
