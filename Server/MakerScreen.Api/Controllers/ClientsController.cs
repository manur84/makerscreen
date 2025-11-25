using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MakerScreen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IWebSocketServer _webSocketServer;
    private readonly IClientMonitorService _clientMonitorService;
    private readonly IClientDeploymentService _deploymentService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        IWebSocketServer webSocketServer,
        IClientMonitorService clientMonitorService,
        IClientDeploymentService deploymentService,
        ILogger<ClientsController> logger)
    {
        _webSocketServer = webSocketServer;
        _clientMonitorService = clientMonitorService;
        _deploymentService = deploymentService;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<SignageClient>> GetAll()
    {
        var clients = _webSocketServer.GetConnectedClients();
        return Ok(clients);
    }

    [HttpGet("{id}")]
    public ActionResult<SignageClient> Get(string id)
    {
        var client = _clientMonitorService.GetClient(id);
        if (client == null)
        {
            return NotFound();
        }
        return Ok(client);
    }

    [HttpGet("status/{status}")]
    public ActionResult<IEnumerable<SignageClient>> GetByStatus(ClientStatus status)
    {
        var clients = _clientMonitorService.GetClientsByStatus(status);
        return Ok(clients);
    }

    [HttpPost("{id}/command")]
    public async Task<ActionResult> SendCommand(string id, [FromBody] CommandRequest request)
    {
        var message = new WebSocketMessage
        {
            Type = MessageTypes.Command,
            ClientId = id,
            Data = new
            {
                command = request.Command,
                parameters = request.Parameters
            }
        };

        await _webSocketServer.SendMessageAsync(id, message);
        return Ok(new { message = "Command sent" });
    }

    [HttpPost("broadcast/command")]
    public async Task<ActionResult> BroadcastCommand([FromBody] CommandRequest request)
    {
        var message = new WebSocketMessage
        {
            Type = MessageTypes.Command,
            Data = new
            {
                command = request.Command,
                parameters = request.Parameters
            }
        };

        await _webSocketServer.BroadcastMessageAsync(message);
        return Ok(new { message = "Command broadcast to all clients" });
    }

    [HttpPost("deploy")]
    public async Task<ActionResult> AutoDeploy()
    {
        var result = await _deploymentService.AutoDiscoverAndDeployAsync();
        return Ok(new { success = result, message = result ? "Deployment initiated" : "No clients found to deploy" });
    }

    [HttpPost("deploy/{ip}")]
    public async Task<ActionResult> DeployToClient(string ip, [FromBody] DeploymentConfiguration config)
    {
        var package = await _deploymentService.CreateDeploymentPackageAsync(config.Configuration ?? new Dictionary<string, string>());
        var result = await _deploymentService.DeployToClientAsync(ip, package);
        return Ok(new { success = result });
    }
}

public class CommandRequest
{
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, string>? Parameters { get; set; }
}

public class DeploymentConfiguration
{
    public Dictionary<string, string>? Configuration { get; set; }
}
