using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MakerScreen.Core.Interfaces;
using MakerScreen.Management.ViewModels;
using MakerScreen.Management.Views;
using MakerScreen.Services.WebSocket;
using MakerScreen.Services.Deployment;
using MakerScreen.Services.Content;
using MakerScreen.Services.Playlist;
using MakerScreen.Services.Overlay;
using MakerScreen.Services.Monitor;
using MakerScreen.Services.Discovery;

namespace MakerScreen.Management;

public partial class App : Application
{
    private Microsoft.Extensions.Hosting.IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set up global exception handling
        SetupExceptionHandling();

        try
        {
            _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register core services
                    services.AddSingleton<IWebSocketServer, SecureWebSocketServer>();
                    services.AddSingleton<IClientDeploymentService, ClientDeploymentService>();
                    services.AddSingleton<IContentService, ContentService>();
                    
                    // Register new services
                    services.AddSingleton<IPlaylistService, PlaylistService>();
                    services.AddSingleton<IOverlayService, OverlayService>();
                    services.AddSingleton<IClientMonitorService, ClientMonitorService>();
                    services.AddSingleton<INetworkDiscoveryService, NetworkDiscoveryService>();

                    // Register ViewModels
                    services.AddTransient<MainViewModel>();

                    // Register Views
                    services.AddTransient<MainWindow>();

                    // Logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                })
                .Build();

            await _host.StartAsync();

            // Start WebSocket server with error handling
            await StartWebSocketServerWithErrorHandling();

            // Show main window
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            HandleStartupException(ex);
        }
    }

    private void SetupExceptionHandling()
    {
        // Handle unhandled exceptions in the UI thread
        DispatcherUnhandledException += (s, e) =>
        {
            ShowErrorDialog(
                "Ein unerwarteter Fehler ist aufgetreten.",
                e.Exception.Message,
                e.Exception);
            e.Handled = true;
        };

        // Handle unhandled exceptions in background threads
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                ShowErrorDialog(
                    "Ein kritischer Fehler ist aufgetreten.",
                    ex.Message,
                    ex);
            }
        };

        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            ShowErrorDialog(
                "Ein Hintergrundfehler ist aufgetreten.",
                e.Exception.Message,
                e.Exception);
            e.SetObserved();
        };
    }

    private async Task StartWebSocketServerWithErrorHandling()
    {
        try
        {
            var webSocketServer = _host!.Services.GetRequiredService<IWebSocketServer>();
            await webSocketServer.StartAsync();
        }
        catch (WebSocketServerException ex)
        {
            // WebSocket server failed to start - offer to restart as admin
            var result = MessageBox.Show(
                $"Der WebSocket-Server konnte nicht gestartet werden.\n\n" +
                $"Fehler: {ex.Message}\n\n" +
                $"Um den Server auf allen Netzwerkschnittstellen zu starten, sind Administratorrechte erforderlich.\n\n" +
                $"Möchten Sie die Anwendung als Administrator neu starten?",
                "WebSocket-Server Fehler",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                RestartAsAdministrator();
            }
            else
            {
                // Continue with localhost-only binding (server already fell back if possible)
                MessageBox.Show(
                    "Die Anwendung wird mit eingeschränkter Funktionalität fortgesetzt.\n" +
                    "Nur lokale Verbindungen werden akzeptiert.",
                    "Eingeschränkter Modus",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"WebSocket-Server konnte nicht gestartet werden: {ex.Message}", ex);
        }
    }

    private void HandleStartupException(Exception ex)
    {
        string message;
        string title = "Startfehler";
        bool offerRestart = false;

        if (ex is UnauthorizedAccessException || 
            ex.InnerException is UnauthorizedAccessException ||
            (ex.Message?.Contains("Zugriff", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (ex.Message?.Contains("Access", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            message = "Die Anwendung konnte nicht gestartet werden, da die erforderlichen Zugriffsrechte fehlen.\n\n" +
                      "Möchten Sie die Anwendung als Administrator neu starten?";
            offerRestart = true;
        }
        else
        {
            message = $"Die Anwendung konnte nicht gestartet werden.\n\nFehler: {ex.Message}";
        }

        var result = MessageBox.Show(
            message,
            title,
            offerRestart ? MessageBoxButton.YesNo : MessageBoxButton.OK,
            MessageBoxImage.Error);

        if (offerRestart && result == MessageBoxResult.Yes)
        {
            RestartAsAdministrator();
        }
        else
        {
            Shutdown(1);
        }
    }

    /// <summary>
    /// Restarts the application with administrator privileges
    /// </summary>
    private void RestartAsAdministrator()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName,
                Verb = "runas" // Request elevation
            };

            Process.Start(processInfo);
            Shutdown(0);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // User cancelled UAC prompt or elevation failed
            if (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
            {
                MessageBox.Show(
                    "Der Neustart als Administrator wurde abgebrochen.\n" +
                    "Die Anwendung wird ohne erhöhte Rechte fortgesetzt.",
                    "Neustart abgebrochen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    $"Der Neustart als Administrator ist fehlgeschlagen.\n\nFehler: {ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Checks if the current process is running with administrator privileges
    /// </summary>
    public static bool IsRunningAsAdministrator()
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

    private void ShowErrorDialog(string title, string message, Exception? exception = null)
    {
        var detailedMessage = exception != null
            ? $"{message}\n\nDetails: {exception.GetType().Name}"
            : message;

        Current?.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                detailedMessage,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        });
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            try
            {
                var webSocketServer = _host.Services.GetRequiredService<IWebSocketServer>();
                await webSocketServer.StopAsync();
            }
            catch
            {
                // Ignore errors during shutdown
            }
            
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
