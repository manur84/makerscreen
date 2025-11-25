using MakerScreen.Core.Interfaces;
using MakerScreen.Services.WebSocket;
using MakerScreen.Services.Content;
using MakerScreen.Services.Deployment;
using MakerScreen.Services.Playlist;
using MakerScreen.Services.Overlay;
using MakerScreen.Services.Monitor;
using MakerScreen.Services.Discovery;

namespace MakerScreen.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "MakerScreen API",
                Version = "v1",
                Description = "Digital Signage Management API"
            });
        });

        // Register core services
        builder.Services.AddSingleton<IWebSocketServer, SecureWebSocketServer>();
        builder.Services.AddSingleton<IContentService, ContentService>();
        builder.Services.AddSingleton<IClientDeploymentService, ClientDeploymentService>();
        builder.Services.AddSingleton<IPlaylistService, PlaylistService>();
        builder.Services.AddSingleton<IOverlayService, OverlayService>();
        builder.Services.AddSingleton<IClientMonitorService, ClientMonitorService>();
        builder.Services.AddSingleton<INetworkDiscoveryService, NetworkDiscoveryService>();

        // Add hosted service for WebSocket server
        builder.Services.AddHostedService<WebSocketHostedService>();

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("AllowAll");
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }
}

public class WebSocketHostedService : IHostedService
{
    private readonly IWebSocketServer _webSocketServer;
    private readonly ILogger<WebSocketHostedService> _logger;

    public WebSocketHostedService(IWebSocketServer webSocketServer, ILogger<WebSocketHostedService> logger)
    {
        _webSocketServer = webSocketServer;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting WebSocket server...");
        await _webSocketServer.StartAsync(cancellationToken);
        _logger.LogInformation("WebSocket server started on port 8443");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping WebSocket server...");
        await _webSocketServer.StopAsync(cancellationToken);
    }
}
