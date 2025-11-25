using MakerScreen.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MakerScreen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private readonly INetworkDiscoveryService _discoveryService;
    private readonly ILogger<DiscoveryController> _logger;

    public DiscoveryController(INetworkDiscoveryService discoveryService, ILogger<DiscoveryController> logger)
    {
        _discoveryService = discoveryService;
        _logger = logger;
    }

    [HttpGet("scan")]
    public async Task<ActionResult<IEnumerable<DiscoveredDevice>>> Scan([FromQuery] int timeoutSeconds = 10)
    {
        var devices = await _discoveryService.DiscoverDevicesAsync(TimeSpan.FromSeconds(timeoutSeconds));
        return Ok(devices);
    }

    [HttpPost("start")]
    public async Task<ActionResult> StartContinuousDiscovery()
    {
        await _discoveryService.StartDiscoveryAsync();
        return Ok(new { message = "Continuous discovery started" });
    }

    [HttpPost("stop")]
    public async Task<ActionResult> StopContinuousDiscovery()
    {
        await _discoveryService.StopDiscoveryAsync();
        return Ok(new { message = "Continuous discovery stopped" });
    }
}
