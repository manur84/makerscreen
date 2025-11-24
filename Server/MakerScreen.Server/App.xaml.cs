using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MakerScreen.Server;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Setup Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/makerscreen.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Setup DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddSerilog();
        });

        // Add services here
        services.AddSingleton<MakerScreen.WebSocket.Services.WebSocketServer>();
    }
}
