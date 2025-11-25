using System.Collections.ObjectModel;
using System.IO;
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
    private readonly IPlaylistService? _playlistService;
    private readonly IOverlayService? _overlayService;
    private readonly IClientMonitorService? _clientMonitorService;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isServerRunning = true;

    [ObservableProperty]
    private SignageClient? _selectedClient;

    [ObservableProperty]
    private ContentItem? _selectedContent;

    [ObservableProperty]
    private Playlist? _selectedPlaylist;

    public ObservableCollection<SignageClient> Clients { get; } = new();
    public ObservableCollection<ContentItem> ContentItems { get; } = new();
    public ObservableCollection<Playlist> Playlists { get; } = new();
    public ObservableCollection<Overlay> Overlays { get; } = new();

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IWebSocketServer webSocketServer,
        IClientDeploymentService deploymentService,
        IContentService contentService,
        IPlaylistService? playlistService = null,
        IOverlayService? overlayService = null,
        IClientMonitorService? clientMonitorService = null)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _deploymentService = deploymentService;
        _contentService = contentService;
        _playlistService = playlistService;
        _overlayService = overlayService;
        _clientMonitorService = clientMonitorService;

        // Subscribe to client monitor events if available
        if (_clientMonitorService != null)
        {
            _clientMonitorService.ClientStatusChanged += OnClientStatusChanged;
            _clientMonitorService.ClientDisconnected += OnClientDisconnected;
        }

        // Start periodic client list refresh
        StartClientRefreshTimer();
#pragma warning disable CS4014
        LoadContent();
        LoadPlaylists();
        LoadOverlays();
#pragma warning restore CS4014
    }

    private void OnClientStatusChanged(object? sender, ClientStatusChangedEventArgs e)
    {
        _logger.LogInformation("Client {ClientId} status changed: {OldStatus} -> {NewStatus}", 
            e.ClientId, e.OldStatus, e.NewStatus);
        
        App.Current.Dispatcher.Invoke(() =>
        {
            var client = Clients.FirstOrDefault(c => c.Id == e.ClientId);
            if (client != null)
            {
                client.Status = e.NewStatus;
            }
        });
    }

    private void OnClientDisconnected(object? sender, string clientId)
    {
        _logger.LogInformation("Client {ClientId} disconnected", clientId);
        StatusMessage = $"Client {clientId} disconnected";
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
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|Video files (*.mp4)|*.mp4|All files (*.*)|*.*",
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
                    Type = DetermineContentType(extension),
                    Data = fileData,
                    MimeType = DetermineMimeType(extension)
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
    private async Task DeleteContent(ContentItem? content)
    {
        if (content == null) return;

        try
        {
            await _contentService.DeleteContentAsync(content.Id);
            await LoadContent();
            StatusMessage = $"Content '{content.Name}' deleted";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting content");
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
    private async Task CreatePlaylist()
    {
        if (_playlistService == null) return;

        try
        {
            var playlist = new Playlist
            {
                Name = $"Playlist {DateTime.Now:yyyy-MM-dd HH:mm}",
                Description = "New playlist"
            };

            await _playlistService.CreatePlaylistAsync(playlist);
            await LoadPlaylists();
            StatusMessage = $"Playlist '{playlist.Name}' created";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating playlist");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddContentToPlaylist(ContentItem? content)
    {
        if (content == null || SelectedPlaylist == null || _playlistService == null) return;

        try
        {
            SelectedPlaylist.Items.Add(new PlaylistItem
            {
                Order = SelectedPlaylist.Items.Count + 1,
                ContentId = content.Id,
                Duration = content.Duration
            });

            await _playlistService.UpdatePlaylistAsync(SelectedPlaylist);
            StatusMessage = $"Added '{content.Name}' to playlist";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding content to playlist");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AssignPlaylistToClients()
    {
        if (SelectedPlaylist == null || _playlistService == null) return;

        try
        {
            var clientIds = Clients.Select(c => c.Id).ToList();
            await _playlistService.AssignPlaylistToClientsAsync(SelectedPlaylist.Id, clientIds);
            StatusMessage = $"Playlist '{SelectedPlaylist.Name}' assigned to all clients";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning playlist to clients");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SendCommandToClient(string command)
    {
        if (SelectedClient == null) return;

        try
        {
            var message = new WebSocketMessage
            {
                Type = MessageTypes.Command,
                ClientId = SelectedClient.Id,
                Data = new { command }
            };

            await _webSocketServer.SendMessageAsync(SelectedClient.Id, message);
            StatusMessage = $"Command '{command}' sent to {SelectedClient.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command to client");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BroadcastCommand(string command)
    {
        try
        {
            var message = new WebSocketMessage
            {
                Type = MessageTypes.Command,
                Data = new { command }
            };

            await _webSocketServer.BroadcastMessageAsync(message);
            StatusMessage = $"Command '{command}' broadcast to all clients";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting command");
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

    private async Task LoadPlaylists()
    {
        if (_playlistService == null) return;

        try
        {
            var playlists = await _playlistService.GetAllPlaylistsAsync();
            
            Playlists.Clear();
            foreach (var playlist in playlists)
            {
                Playlists.Add(playlist);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading playlists");
        }
    }

    private async Task LoadOverlays()
    {
        if (_overlayService == null) return;

        try
        {
            var overlays = await _overlayService.GetAllOverlaysAsync();
            
            Overlays.Clear();
            foreach (var overlay in overlays)
            {
                Overlays.Add(overlay);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading overlays");
        }
    }

    private ContentType DetermineContentType(string extension)
    {
        return extension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" => ContentType.Image,
            ".mp4" or ".webm" => ContentType.Video,
            ".html" => ContentType.Html,
            _ => ContentType.Image
        };
    }

    private string DetermineMimeType(string extension)
    {
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".html" => "text/html",
            _ => "application/octet-stream"
        };
    }
}
