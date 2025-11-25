using MakerScreen.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MakerScreen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IWebSocketServer _webSocketServer;
    private readonly IClientMonitorService _clientMonitorService;

    public SystemController(IWebSocketServer webSocketServer, IClientMonitorService clientMonitorService)
    {
        _webSocketServer = webSocketServer;
        _clientMonitorService = clientMonitorService;
    }

    [HttpGet("status")]
    public ActionResult GetStatus()
    {
        var clients = _webSocketServer.GetConnectedClients();
        
        return Ok(new
        {
            status = "running",
            version = "1.0.0",
            uptime = Environment.TickCount64 / 1000,
            connectedClients = clients.Count,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("health")]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            healthy = true,
            checks = new
            {
                websocket = "ok",
                contentService = "ok",
                database = "ok"
            }
        });
    }
}
