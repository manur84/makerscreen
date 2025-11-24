using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MakerScreen.Core.Interfaces;
using MakerScreen.Services.WebSocket;
using MakerScreen.Services.Deployment;
using MakerScreen.Services.Content;

namespace MakerScreen.Server;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("MakerScreen Digital Signage Server");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddSingleton<IWebSocketServer, SecureWebSocketServer>();
                services.AddSingleton<IClientDeploymentService, ClientDeploymentService>();
                services.AddSingleton<IContentService, ContentService>();
                
                // Register hosted service
                services.AddHostedService<ServerHostedService>();
                
                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();

        await host.RunAsync();
    }
}

class ServerHostedService : IHostedService
{
    private readonly ILogger<ServerHostedService> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly IClientDeploymentService _deploymentService;

    public ServerHostedService(
        ILogger<ServerHostedService> logger,
        IWebSocketServer webSocketServer,
        IClientDeploymentService deploymentService)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _deploymentService = deploymentService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MakerScreen Server...");
        
        // Start WebSocket server
        await _webSocketServer.StartAsync(cancellationToken);
        
        _logger.LogInformation("Server started successfully!");
        _logger.LogInformation("WebSocket server listening on port 8443");
        _logger.LogInformation("Use the Management Console to deploy clients");
        
        // Optionally start auto-discovery
        // await _deploymentService.AutoDiscoverAndDeployAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MakerScreen Server...");
        await _webSocketServer.StopAsync(cancellationToken);
        _logger.LogInformation("Server stopped");
    }
}
