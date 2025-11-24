using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace MakerScreen.Management.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly IClientDeploymentService _deploymentService;
    private readonly IContentService _contentService;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isServerRunning = true;

    public ObservableCollection<SignageClient> Clients { get; } = new();
    public ObservableCollection<ContentItem> ContentItems { get; } = new();

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IWebSocketServer webSocketServer,
        IClientDeploymentService deploymentService,
        IContentService contentService)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _deploymentService = deploymentService;
        _contentService = contentService;

        // Start periodic client list refresh
        StartClientRefreshTimer();
        LoadContent();
    }

    [RelayCommand]
    private async Task AutoDeployClients()
    {
        try
        {
            StatusMessage = "Auto-discovering and deploying clients...";
            _logger.LogInformation("Starting auto-deployment");

            var success = await _deploymentService.AutoDiscoverAndDeployAsync();

            if (success)
            {
                StatusMessage = "Auto-deployment completed successfully!";
            }
            else
            {
                StatusMessage = "Auto-deployment completed with some failures. Check logs.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-deployment");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateDeploymentPackage()
    {
        try
        {
            StatusMessage = "Creating deployment package...";

            var config = new Dictionary<string, string>
            {
                ["serverUrl"] = "ws://YOUR_SERVER_IP:8443",
                ["autoStart"] = "true"
            };

            var package = await _deploymentService.CreateDeploymentPackageAsync(config);

            StatusMessage = $"Deployment package created! Hash: {package.Hash}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deployment package");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GenerateRaspberryPiImage()
    {
        try
        {
            StatusMessage = "Generating Raspberry Pi image...";

            var config = new Dictionary<string, string>
            {
                ["serverUrl"] = "ws://YOUR_SERVER_IP:8443",
                ["autoStart"] = "true"
            };

            var package = await _deploymentService.CreateDeploymentPackageAsync(config);
            var imagePath = await _deploymentService.GenerateRaspberryPiImageAsync(package);

            StatusMessage = $"Image generated: {imagePath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddContent()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
                Title = "Select Content File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var fileData = await File.ReadAllBytesAsync(openFileDialog.FileName);
                var fileName = Path.GetFileName(openFileDialog.FileName);
                var extension = Path.GetExtension(openFileDialog.FileName).ToLower();

                var contentItem = new ContentItem
                {
                    Name = fileName,
                    Type = ContentType.Image,
                    Data = fileData,
                    MimeType = extension switch
                    {
                        ".png" => "image/png",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".gif" => "image/gif",
                        _ => "application/octet-stream"
                    }
                };

                await _contentService.AddContentAsync(contentItem);
                await LoadContent();

                StatusMessage = $"Content '{fileName}' added successfully";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding content");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PushContentToClients(ContentItem? content)
    {
        if (content == null) return;

        try
        {
            StatusMessage = $"Pushing content '{content.Name}' to all clients...";
            await _contentService.PushContentToClientsAsync(content.Id);
            StatusMessage = $"Content '{content.Name}' pushed successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing content");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshClients()
    {
        await Task.Run(() =>
        {
            var clients = _webSocketServer.GetConnectedClients();
            
            App.Current.Dispatcher.Invoke(() =>
            {
                Clients.Clear();
                foreach (var client in clients)
                {
                    Clients.Add(client);
                }
            });

            StatusMessage = $"Found {clients.Count} connected client(s)";
        });
    }

    private async void StartClientRefreshTimer()
    {
        while (true)
        {
            await Task.Delay(5000); // Refresh every 5 seconds
            await RefreshClients();
        }
    }

    private async Task LoadContent()
    {
        try
        {
            var content = await _contentService.GetAllContentAsync();
            
            ContentItems.Clear();
            foreach (var item in content)
            {
                ContentItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading content");
        }
    }
}
