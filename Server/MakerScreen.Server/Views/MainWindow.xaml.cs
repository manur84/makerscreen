using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Server.Views;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly MakerScreen.WebSocket.Services.WebSocketServer _webSocketServer;

    public MainWindow()
    {
        InitializeComponent();
        _logger = App.ServiceProvider.GetRequiredService<ILogger<MainWindow>>();
        _webSocketServer = App.ServiceProvider.GetRequiredService<MakerScreen.WebSocket.Services.WebSocketServer>();
        
        LoadData();
    }

    private void LoadData()
    {
        // TODO: Load from database
        OnlineClientsText.Text = "0";
        TotalClientsText.Text = "0";
        ContentCountText.Text = "0";
    }

    private void UploadContent_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PNG Files (*.png)|*.png",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                _logger.LogInformation("Uploading content: {File}", file);
                // TODO: Upload to database
            }
            StatusText.Text = $"Uploaded {dialog.FileNames.Length} files";
        }
    }

    private void RefreshContent_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Refreshing content list");
        // TODO: Reload from database
        StatusText.Text = "Content refreshed";
    }

    private void RefreshClients_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Refreshing clients list");
        // TODO: Reload from database
        StatusText.Text = "Clients refreshed";
    }

    private void BuildImage_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Starting image build");
        MessageBox.Show("Raspberry Pi image builder will be implemented here", 
                       "Image Builder", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void StartWebSocketServer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var port = WebSocketPortText.Text;
            _logger.LogInformation("Starting WebSocket server on port {Port}", port);
            
            _ = Task.Run(async () => await _webSocketServer.StartAsync($"http://localhost:{port}/ws/"));
            
            ServerStatusText.Text = "Server Status: Running";
            StatusText.Text = "WebSocket server started";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WebSocket server");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
