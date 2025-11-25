using System.Diagnostics;
using System.Security.Principal;
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
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("MakerScreen Digital Signage Server");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Check for admin rights and display status
        var isAdmin = IsRunningAsAdministrator();
        Console.WriteLine($"Administrator-Modus: {(isAdmin ? "Ja" : "Nein")}");
        Console.WriteLine();

        try
        {
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
            return 0;
        }
        catch (WebSocketServerException ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("WARNUNG: WebSocket-Server konnte nicht vollständig gestartet werden.");
            Console.WriteLine($"Fehler: {ex.Message}");
            Console.WriteLine();
            Console.ResetColor();

            if (!isAdmin)
            {
                Console.WriteLine("Hinweis: Um den Server auf allen Netzwerkschnittstellen zu starten,");
                Console.WriteLine("führen Sie die Anwendung als Administrator aus:");
                Console.WriteLine();
                Console.WriteLine("  1. Rechtsklick auf die Anwendung -> 'Als Administrator ausführen'");
                Console.WriteLine("  2. Oder im Terminal: ");
                Console.WriteLine("     runas /user:Administrator \"dotnet run\"");
                Console.WriteLine();

                if (OfferElevation())
                {
                    RestartAsAdministrator();
                    return 0;
                }
            }

            return 1;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("FEHLER: Die Anwendung konnte nicht gestartet werden.");
            Console.WriteLine($"Details: {ex.Message}");
            Console.WriteLine();
            Console.ResetColor();

            if (ex is UnauthorizedAccessException || 
                (ex.Message?.Contains("Zugriff", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (ex.Message?.Contains("Access", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                Console.WriteLine("Dieser Fehler kann durch fehlende Berechtigungen verursacht werden.");
                Console.WriteLine("Versuchen Sie, die Anwendung als Administrator auszuführen.");
                Console.WriteLine();

                if (!isAdmin && OfferElevation())
                {
                    RestartAsAdministrator();
                    return 0;
                }
            }

            return 1;
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static bool OfferElevation()
    {
        Console.WriteLine("Möchten Sie die Anwendung als Administrator neu starten? (j/n)");
        var key = Console.ReadKey(true);
        return key.KeyChar == 'j' || key.KeyChar == 'J' || key.KeyChar == 'y' || key.KeyChar == 'Y';
    }

    private static void RestartAsAdministrator()
    {
        try
        {
            Console.WriteLine("Neustart als Administrator...");
            
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName,
                Verb = "runas" // Request elevation
            };

            Process.Start(processInfo);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            if (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
            {
                Console.WriteLine("Der Neustart als Administrator wurde abgebrochen.");
            }
            else
            {
                Console.WriteLine($"Fehler beim Neustart: {ex.Message}");
            }
        }
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
        
        try
        {
            // Start WebSocket server
            await _webSocketServer.StartAsync(cancellationToken);
            
            _logger.LogInformation("Server started successfully!");
            
            // Log binding information
            if (_webSocketServer is SecureWebSocketServer secureServer)
            {
                var bindingAddress = secureServer.ActualBindingAddress;
                if (bindingAddress?.Contains("+") == true)
                {
                    _logger.LogInformation("WebSocket server listening on ALL interfaces, port 8443");
                    _logger.LogInformation("Remote clients can connect to this server");
                }
                else
                {
                    _logger.LogWarning("WebSocket server listening on localhost only, port 8443");
                    _logger.LogWarning("Only local connections are accepted. Run as Administrator for network access.");
                }
            }
            else
            {
                _logger.LogInformation("WebSocket server listening on port 8443");
            }
            
            _logger.LogInformation("Use the Management Console to deploy clients");
        }
        catch (WebSocketServerException)
        {
            // Re-throw to be handled by the main program
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WebSocket server");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MakerScreen Server...");
        
        try
        {
            await _webSocketServer.StopAsync(cancellationToken);
            _logger.LogInformation("Server stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping server");
        }
    }
}
