using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MakerScreen.Core.Interfaces;
using MakerScreen.Management.ViewModels;
using MakerScreen.Management.Views;
using MakerScreen.Services.WebSocket;
using MakerScreen.Services.Deployment;
using MakerScreen.Services.Content;

namespace MakerScreen.Management;

public partial class App : Application
{
    private Microsoft.Extensions.Hosting.IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddSingleton<IWebSocketServer, SecureWebSocketServer>();
                services.AddSingleton<IClientDeploymentService, ClientDeploymentService>();
                services.AddSingleton<IContentService, ContentService>();

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

        // Start WebSocket server
        var webSocketServer = _host.Services.GetRequiredService<IWebSocketServer>();
        await webSocketServer.StartAsync();

        // Show main window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            var webSocketServer = _host.Services.GetRequiredService<IWebSocketServer>();
            await webSocketServer.StopAsync();
            
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
