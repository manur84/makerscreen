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
    private const int ALIGNMENT_MARGIN = 20;
    private const int MAX_RESOLUTION = 7680; // 8K max resolution
    private const int MIN_RESOLUTION = 100;
    
    private readonly ILogger<MainViewModel> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly IClientDeploymentService _deploymentService;
    private readonly IContentService _contentService;
    private readonly IPlaylistService? _playlistService;
    private readonly IOverlayService? _overlayService;
    private readonly IClientMonitorService? _clientMonitorService;
    private readonly IClientGroupService? _clientGroupService;
    private readonly IEmergencyBroadcastService? _emergencyBroadcastService;
    private readonly IDisplayCompositionService? _compositionService;

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

    [ObservableProperty]
    private Overlay? _selectedOverlay;

    [ObservableProperty]
    private ClientGroup? _selectedGroup;

    [ObservableProperty]
    private EmergencyBroadcast? _selectedEmergencyBroadcast;

    [ObservableProperty]
    private DisplayComposition? _selectedComposition;

    [ObservableProperty]
    private CompositionOverlay? _selectedCompositionOverlay;

    [ObservableProperty]
    private string _selectedResolutionPreset = "Full HD (1920x1080)";

    [ObservableProperty]
    private int _customResolutionWidth = 1920;

    [ObservableProperty]
    private int _customResolutionHeight = 1080;

    [ObservableProperty]
    private string _selectedScaleMode = "Fill";

    [ObservableProperty]
    private bool _isPreviewMode = false;

    /// <summary>
    /// Zoom level for the scene editor canvas (range: 0.1 to 2.0)
    /// </summary>
    [ObservableProperty]
    private double _zoomLevel = 0.35;

    /// <summary>
    /// Server URL for Raspberry Pi deployment. Set to auto-detect if left empty or at default.
    /// </summary>
    [ObservableProperty]
    private string _raspberryPiServerUrl = "ws://localhost:8443";

    public ObservableCollection<SignageClient> Clients { get; } = new();
    public ObservableCollection<ContentItem> ContentItems { get; } = new();
    public ObservableCollection<Playlist> Playlists { get; } = new();
    public ObservableCollection<Overlay> Overlays { get; } = new();
    public ObservableCollection<ClientGroup> ClientGroups { get; } = new();
    public ObservableCollection<EmergencyBroadcast> EmergencyBroadcasts { get; } = new();
    public ObservableCollection<DisplayComposition> Compositions { get; } = new();
    public ObservableCollection<SceneTemplate> SceneTemplates { get; } = new();
    public ObservableCollection<Widget> Widgets { get; } = new();
    
    /// <summary>
    /// Available resolution presets for the scene editor
    /// </summary>
    public List<string> ResolutionPresetOptions { get; } = new List<string>(MakerScreen.Core.Models.ResolutionPresets.Presets.Keys);

    /// <summary>
    /// Available scale modes for background images
    /// </summary>
    public List<string> ScaleModeOptions { get; } = new List<string> { "Fill", "Fit", "Stretch", "Center", "Tile" };

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IWebSocketServer webSocketServer,
        IClientDeploymentService deploymentService,
        IContentService contentService,
        IPlaylistService? playlistService = null,
        IOverlayService? overlayService = null,
        IClientMonitorService? clientMonitorService = null,
        IClientGroupService? clientGroupService = null,
        IEmergencyBroadcastService? emergencyBroadcastService = null,
        IDisplayCompositionService? compositionService = null)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _deploymentService = deploymentService;
        _contentService = contentService;
        _playlistService = playlistService;
        _overlayService = overlayService;
        _clientMonitorService = clientMonitorService;
        _clientGroupService = clientGroupService;
        _emergencyBroadcastService = emergencyBroadcastService;
        _compositionService = compositionService;

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
        LoadClientGroups();
        LoadEmergencyBroadcasts();
        LoadCompositions();
#pragma warning restore CS4014
        LoadSceneTemplates();
        LoadWidgets();
    }

    private void LoadWidgets()
    {
        Widgets.Clear();
        foreach (var widget in DefaultSceneTemplates.GetWidgets())
        {
            Widgets.Add(widget);
        }
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

            // Use the configured server URL from the ViewModel
            var serverUrl = RaspberryPiServerUrl;
            if (string.IsNullOrWhiteSpace(serverUrl) || serverUrl == "ws://localhost:8443")
            {
                // Auto-detect local server IP asynchronously when using default localhost
                serverUrl = await GetLocalServerUrlAsync();
            }

            var config = new Dictionary<string, string>
            {
                ["serverUrl"] = serverUrl,
                ["autoStart"] = "true",
                ["clientVersion"] = "1.0.0",
                ["displayRotation"] = "0",
                ["fullscreen"] = "true",
                ["enableHdmiCec"] = "true",
                ["enableLocalWebUi"] = "true",
                ["localWebUiPort"] = "5000",
                ["logLevel"] = "INFO",
                ["heartbeatInterval"] = "30",
                ["reconnectDelay"] = "5"
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

    #region Overlay Commands

    [RelayCommand]
    private async Task CreateOverlay()
    {
        if (_overlayService == null) return;

        try
        {
            var overlay = new Overlay
            {
                Name = $"Overlay {DateTime.Now:yyyy-MM-dd HH:mm}",
                Type = OverlayType.Text,
                Content = "New Overlay"
            };

            await _overlayService.CreateOverlayAsync(overlay);
            await LoadOverlays();
            SelectedOverlay = overlay;
            StatusMessage = $"Overlay '{overlay.Name}' created";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating overlay");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveOverlay()
    {
        if (SelectedOverlay == null || _overlayService == null) return;

        try
        {
            await _overlayService.UpdateOverlayAsync(SelectedOverlay);
            StatusMessage = $"Overlay '{SelectedOverlay.Name}' saved";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving overlay");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteOverlay(Overlay? overlay)
    {
        if (overlay == null || _overlayService == null) return;

        try
        {
            await _overlayService.DeleteOverlayAsync(overlay.Id);
            await LoadOverlays();
            SelectedOverlay = null;
            StatusMessage = $"Overlay '{overlay.Name}' deleted";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting overlay");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AssignOverlayToClients()
    {
        if (SelectedOverlay == null || _overlayService == null) return;

        try
        {
            var clientIds = Clients.Select(c => c.Id).ToList();
            await _overlayService.AssignOverlayToClientsAsync(SelectedOverlay.Id, clientIds);
            StatusMessage = $"Overlay '{SelectedOverlay.Name}' assigned to all clients";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning overlay to clients");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Playlist Commands

    [RelayCommand]
    private async Task DeletePlaylist(Playlist? playlist)
    {
        if (playlist == null || _playlistService == null) return;

        try
        {
            await _playlistService.DeletePlaylistAsync(playlist.Id);
            await LoadPlaylists();
            SelectedPlaylist = null;
            StatusMessage = $"Playlist '{playlist.Name}' deleted";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting playlist");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AssignPlaylistToGroup()
    {
        if (SelectedPlaylist == null || SelectedGroup == null || _clientGroupService == null) return;

        try
        {
            await _clientGroupService.AssignPlaylistToGroupAsync(SelectedGroup.Id, SelectedPlaylist.Id);
            StatusMessage = $"Playlist '{SelectedPlaylist.Name}' assigned to group '{SelectedGroup.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning playlist to group");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Client Group Commands

    [RelayCommand]
    private async Task CreateGroup()
    {
        if (_clientGroupService == null) return;

        try
        {
            var group = new ClientGroup
            {
                Name = $"Group {DateTime.Now:yyyy-MM-dd HH:mm}",
                Description = "New client group"
            };

            await _clientGroupService.CreateGroupAsync(group);
            await LoadClientGroups();
            SelectedGroup = group;
            StatusMessage = $"Group '{group.Name}' created";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteGroup(ClientGroup? group)
    {
        if (group == null || _clientGroupService == null) return;

        try
        {
            await _clientGroupService.DeleteGroupAsync(group.Id);
            await LoadClientGroups();
            SelectedGroup = null;
            StatusMessage = $"Group '{group.Name}' deleted";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddClientToGroup()
    {
        if (SelectedClient == null || SelectedGroup == null || _clientGroupService == null) return;

        try
        {
            await _clientGroupService.AddClientToGroupAsync(SelectedGroup.Id, SelectedClient.Id);
            await LoadClientGroups();
            StatusMessage = $"Client '{SelectedClient.Name}' added to group '{SelectedGroup.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding client to group");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PushContentToGroup()
    {
        if (SelectedContent == null || SelectedGroup == null || _clientGroupService == null) return;

        try
        {
            await _clientGroupService.PushContentToGroupAsync(SelectedGroup.Id, SelectedContent.Id);
            StatusMessage = $"Content pushed to group '{SelectedGroup.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing content to group");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task LoadClientGroups()
    {
        if (_clientGroupService == null) return;

        try
        {
            var groups = await _clientGroupService.GetAllGroupsAsync();
            
            ClientGroups.Clear();
            foreach (var group in groups)
            {
                ClientGroups.Add(group);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading client groups");
        }
    }

    #endregion

    #region Emergency Broadcast Commands

    [RelayCommand]
    private async Task CreateEmergencyBroadcast()
    {
        if (_emergencyBroadcastService == null) return;

        try
        {
            var broadcast = new EmergencyBroadcast
            {
                Title = "Emergency Alert",
                Message = "Enter your emergency message here",
                Priority = EmergencyPriority.High,
                Type = EmergencyType.Alert
            };

            await _emergencyBroadcastService.CreateBroadcastAsync(broadcast);
            await LoadEmergencyBroadcasts();
            SelectedEmergencyBroadcast = broadcast;
            StatusMessage = "Emergency broadcast created";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating emergency broadcast");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SendEmergencyBroadcast()
    {
        if (SelectedEmergencyBroadcast == null || _emergencyBroadcastService == null) return;

        try
        {
            await _emergencyBroadcastService.SendBroadcastAsync(SelectedEmergencyBroadcast.Id);
            StatusMessage = $"EMERGENCY BROADCAST SENT: {SelectedEmergencyBroadcast.Title}";
            _logger.LogWarning("Emergency broadcast sent: {Title}", SelectedEmergencyBroadcast.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending emergency broadcast");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SendEmergencyToGroup()
    {
        if (SelectedEmergencyBroadcast == null || SelectedGroup == null || _emergencyBroadcastService == null) return;

        try
        {
            await _emergencyBroadcastService.SendBroadcastToGroupAsync(SelectedEmergencyBroadcast.Id, SelectedGroup.Id);
            StatusMessage = $"Emergency broadcast sent to group '{SelectedGroup.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending emergency to group");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ClearEmergencyBroadcast(EmergencyBroadcast? broadcast)
    {
        if (broadcast == null || _emergencyBroadcastService == null) return;

        try
        {
            await _emergencyBroadcastService.ClearBroadcastAsync(broadcast.Id);
            await LoadEmergencyBroadcasts();
            StatusMessage = $"Emergency broadcast '{broadcast.Title}' cleared";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing emergency broadcast");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ClearAllEmergencyBroadcasts()
    {
        if (_emergencyBroadcastService == null) return;

        try
        {
            await _emergencyBroadcastService.ClearAllBroadcastsAsync();
            await LoadEmergencyBroadcasts();
            StatusMessage = "All emergency broadcasts cleared";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all emergency broadcasts");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task LoadEmergencyBroadcasts()
    {
        if (_emergencyBroadcastService == null) return;

        try
        {
            var broadcasts = await _emergencyBroadcastService.GetAllBroadcastsAsync();
            
            EmergencyBroadcasts.Clear();
            foreach (var broadcast in broadcasts)
            {
                EmergencyBroadcasts.Add(broadcast);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading emergency broadcasts");
        }
    }

    #endregion

    #region Scene Composition Commands

    private async Task LoadCompositions()
    {
        if (_compositionService == null) return;

        try
        {
            var compositions = await _compositionService.GetAllCompositionsAsync();
            
            Compositions.Clear();
            foreach (var composition in compositions)
            {
                Compositions.Add(composition);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading compositions");
        }
    }

    [RelayCommand]
    private async Task CreateComposition()
    {
        if (_compositionService == null) return;

        try
        {
            var composition = new DisplayComposition
            {
                Name = $"Scene {DateTime.Now:yyyy-MM-dd HH:mm}",
                Description = "New scene composition"
            };

            await _compositionService.CreateCompositionAsync(composition);
            await LoadCompositions();
            SelectedComposition = composition;
            StatusMessage = $"Scene '{composition.Name}' created";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating composition");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveComposition()
    {
        if (SelectedComposition == null || _compositionService == null) return;

        try
        {
            await _compositionService.UpdateCompositionAsync(SelectedComposition);
            StatusMessage = $"Scene '{SelectedComposition.Name}' saved";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving composition");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteComposition(DisplayComposition? composition)
    {
        if (composition == null || _compositionService == null) return;

        try
        {
            await _compositionService.DeleteCompositionAsync(composition.Id);
            await LoadCompositions();
            SelectedComposition = null;
            StatusMessage = $"Scene '{composition.Name}' deleted";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting composition");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SetCompositionBackground()
    {
        if (SelectedComposition == null) return;

        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files (*.*)|*.*",
                Title = "Select Background Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var imageData = await File.ReadAllBytesAsync(openFileDialog.FileName);
                var fileName = Path.GetFileName(openFileDialog.FileName);
                var extension = Path.GetExtension(openFileDialog.FileName).ToLower();
                
                // First update the composition with the new background
                SelectedComposition.Background.Type = BackgroundType.Image;
                SelectedComposition.Background.ImageData = imageData;
                
                if (_compositionService != null)
                {
                    await _compositionService.UpdateCompositionAsync(SelectedComposition);
                }
                
                // Only add as content item after composition update succeeds
                var contentItem = new ContentItem
                {
                    Name = $"bg_{fileName}",
                    Type = ContentType.Image,
                    Data = imageData,
                    MimeType = DetermineMimeType(extension)
                };
                await _contentService.AddContentAsync(contentItem);
                SelectedComposition.Background.ImageContentId = contentItem.Id;
                
                // Update composition again with content reference
                if (_compositionService != null)
                {
                    await _compositionService.UpdateCompositionAsync(SelectedComposition);
                }
                
                // Trigger property changed notification
                OnPropertyChanged(nameof(SelectedComposition));
                
                StatusMessage = $"Background image '{fileName}' set";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting background image");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ClearCompositionBackground()
    {
        if (SelectedComposition == null || _compositionService == null) return;

        try
        {
            SelectedComposition.Background.Type = BackgroundType.Color;
            SelectedComposition.Background.ImageData = null;
            SelectedComposition.Background.ImageContentId = null;
            
            await _compositionService.UpdateCompositionAsync(SelectedComposition);
            OnPropertyChanged(nameof(SelectedComposition));
            StatusMessage = "Background cleared";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing background");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SetCompositionResolution()
    {
        if (SelectedComposition == null || _compositionService == null) return;

        try
        {
            if (MakerScreen.Core.Models.ResolutionPresets.Presets.TryGetValue(SelectedResolutionPreset, out var resolution))
            {
                if (resolution.Width > 0 && resolution.Height > 0)
                {
                    SelectedComposition.Resolution.Width = resolution.Width;
                    SelectedComposition.Resolution.Height = resolution.Height;
                    SelectedComposition.Resolution.PresetName = SelectedResolutionPreset;
                    
                    await _compositionService.UpdateCompositionAsync(SelectedComposition);
                    OnPropertyChanged(nameof(SelectedComposition));
                    StatusMessage = $"Resolution set to {resolution.Width}x{resolution.Height}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting resolution");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddOverlayToComposition()
    {
        if (SelectedComposition == null || SelectedOverlay == null || _compositionService == null) return;

        try
        {
            var compositionOverlay = new CompositionOverlay
            {
                OverlayId = SelectedOverlay.Id,
                Name = SelectedOverlay.Name,
                X = 50,
                Y = 50,
                Width = SelectedOverlay.Position.Width,
                Height = SelectedOverlay.Position.Height
            };

            await _compositionService.AddOverlayToCompositionAsync(SelectedComposition.Id, compositionOverlay);
            OnPropertyChanged(nameof(SelectedComposition));
            StatusMessage = $"Overlay '{SelectedOverlay.Name}' added to scene";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding overlay to composition");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RemoveOverlayFromComposition()
    {
        if (SelectedComposition == null || SelectedCompositionOverlay == null || _compositionService == null) return;

        try
        {
            await _compositionService.RemoveOverlayFromCompositionAsync(SelectedComposition.Id, SelectedCompositionOverlay.Id);
            SelectedCompositionOverlay = null;
            OnPropertyChanged(nameof(SelectedComposition));
            StatusMessage = "Overlay removed from scene";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing overlay from composition");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UpdateCompositionOverlayPosition(CompositionOverlay? overlay)
    {
        if (SelectedComposition == null || overlay == null || _compositionService == null) return;

        try
        {
            await _compositionService.UpdateOverlayInCompositionAsync(SelectedComposition.Id, overlay);
            StatusMessage = "Overlay position updated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overlay position");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PublishComposition()
    {
        if (SelectedComposition == null || _compositionService == null) return;

        try
        {
            await _compositionService.PublishCompositionAsync(SelectedComposition.Id);
            StatusMessage = $"Scene '{SelectedComposition.Name}' published to all displays";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing composition");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PublishCompositionToGroup()
    {
        if (SelectedComposition == null || SelectedGroup == null || _compositionService == null) return;

        try
        {
            await _compositionService.PublishCompositionToGroupAsync(SelectedComposition.Id, SelectedGroup.Id);
            StatusMessage = $"Scene '{SelectedComposition.Name}' published to group '{SelectedGroup.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing composition to group");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void MoveOverlayUp()
    {
        if (SelectedCompositionOverlay == null || SelectedComposition == null) return;
        if (SelectedComposition.Overlays.Count == 0) return;
        
        var maxZIndex = SelectedComposition.Overlays.Max(o => o.ZIndex);
        if (SelectedCompositionOverlay.ZIndex < maxZIndex)
        {
            SelectedCompositionOverlay.ZIndex++;
            OnPropertyChanged(nameof(SelectedComposition));
        }
    }

    [RelayCommand]
    private void MoveOverlayDown()
    {
        if (SelectedCompositionOverlay == null) return;
        
        if (SelectedCompositionOverlay.ZIndex > 0)
        {
            SelectedCompositionOverlay.ZIndex--;
            OnPropertyChanged(nameof(SelectedComposition));
        }
    }

    [RelayCommand]
    private void ToggleOverlayVisibility()
    {
        if (SelectedCompositionOverlay == null) return;
        
        SelectedCompositionOverlay.IsVisible = !SelectedCompositionOverlay.IsVisible;
        OnPropertyChanged(nameof(SelectedComposition));
    }

    [RelayCommand]
    private void ToggleOverlayLock()
    {
        if (SelectedCompositionOverlay == null) return;
        
        SelectedCompositionOverlay.IsLocked = !SelectedCompositionOverlay.IsLocked;
        OnPropertyChanged(nameof(SelectedComposition));
    }

    [RelayCommand]
    private async Task DuplicateComposition()
    {
        if (SelectedComposition == null || _compositionService == null) return;

        try
        {
            var duplicate = new DisplayComposition
            {
                Name = $"{SelectedComposition.Name} (Copy)",
                Description = SelectedComposition.Description,
                Resolution = new CompositionResolution
                {
                    Width = SelectedComposition.Resolution.Width,
                    Height = SelectedComposition.Resolution.Height,
                    PresetName = SelectedComposition.Resolution.PresetName
                },
                Background = new CompositionBackground
                {
                    Type = SelectedComposition.Background.Type,
                    Color = SelectedComposition.Background.Color,
                    ImageContentId = SelectedComposition.Background.ImageContentId,
                    ImageData = SelectedComposition.Background.ImageData,
                    ScaleMode = SelectedComposition.Background.ScaleMode
                },
                Overlays = SelectedComposition.Overlays.Select(o => new CompositionOverlay
                {
                    OverlayId = o.OverlayId,
                    Name = o.Name,
                    X = o.X,
                    Y = o.Y,
                    Width = o.Width,
                    Height = o.Height,
                    ZIndex = o.ZIndex,
                    IsVisible = o.IsVisible,
                    IsLocked = o.IsLocked
                }).ToList()
            };

            await _compositionService.CreateCompositionAsync(duplicate);
            await LoadCompositions();
            SelectedComposition = duplicate;
            StatusMessage = $"Scene duplicated as '{duplicate.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating composition");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ApplyCustomResolution()
    {
        if (SelectedComposition == null || _compositionService == null) return;

        try
        {
            // Validate resolution bounds
            if (CustomResolutionWidth < MIN_RESOLUTION || CustomResolutionHeight < MIN_RESOLUTION)
            {
                StatusMessage = $"Resolution must be at least {MIN_RESOLUTION}x{MIN_RESOLUTION}";
                return;
            }
            
            if (CustomResolutionWidth > MAX_RESOLUTION || CustomResolutionHeight > MAX_RESOLUTION)
            {
                StatusMessage = $"Resolution cannot exceed {MAX_RESOLUTION}x{MAX_RESOLUTION}";
                return;
            }
            
            SelectedComposition.Resolution.Width = CustomResolutionWidth;
            SelectedComposition.Resolution.Height = CustomResolutionHeight;
            SelectedComposition.Resolution.PresetName = $"Custom ({CustomResolutionWidth}x{CustomResolutionHeight})";
            
            await _compositionService.UpdateCompositionAsync(SelectedComposition);
            OnPropertyChanged(nameof(SelectedComposition));
            StatusMessage = $"Custom resolution set to {CustomResolutionWidth}x{CustomResolutionHeight}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying custom resolution");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ApplyBackgroundScaleMode()
    {
        if (SelectedComposition == null || _compositionService == null) return;

        try
        {
            SelectedComposition.Background.ScaleMode = SelectedScaleMode switch
            {
                "Fill" => ImageScaleMode.Fill,
                "Fit" => ImageScaleMode.Fit,
                "Stretch" => ImageScaleMode.Stretch,
                "Center" => ImageScaleMode.Center,
                "Tile" => ImageScaleMode.Tile,
                _ => ImageScaleMode.Fill
            };
            
            await _compositionService.UpdateCompositionAsync(SelectedComposition);
            OnPropertyChanged(nameof(SelectedComposition));
            StatusMessage = $"Background scale mode set to {SelectedScaleMode}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying scale mode");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateFromTemplate(SceneTemplate? template)
    {
        if (template == null || _compositionService == null) return;

        try
        {
            var composition = new DisplayComposition
            {
                Name = $"{template.Name} - {DateTime.Now:yyyy-MM-dd HH:mm}",
                Description = template.Description,
                Resolution = new CompositionResolution
                {
                    Width = template.Resolution.Width,
                    Height = template.Resolution.Height,
                    PresetName = template.Resolution.PresetName
                },
                Background = new CompositionBackground
                {
                    Type = template.Background.Type,
                    Color = template.Background.Color,
                    ScaleMode = template.Background.ScaleMode
                }
            };

            // Create the composition first
            await _compositionService.CreateCompositionAsync(composition);

            // Add predefined overlays from template
            if (_overlayService != null)
            {
                foreach (var placement in template.OverlayPlacements)
                {
                    // Create the overlay
                    var overlay = new Overlay
                    {
                        Name = placement.Name,
                        Type = placement.OverlayType,
                        Content = placement.DefaultContent,
                        Position = new OverlayPosition
                        {
                            X = (int)placement.X,
                            Y = (int)placement.Y,
                            Width = (int)placement.Width,
                            Height = (int)placement.Height
                        },
                        Style = placement.Style
                    };

                    await _overlayService.CreateOverlayAsync(overlay);

                    // Add to composition
                    var compositionOverlay = new CompositionOverlay
                    {
                        OverlayId = overlay.Id,
                        Name = placement.Name,
                        X = placement.X,
                        Y = placement.Y,
                        Width = placement.Width,
                        Height = placement.Height,
                        ZIndex = placement.ZIndex
                    };

                    await _compositionService.AddOverlayToCompositionAsync(composition.Id, compositionOverlay);
                }
            }

            await LoadCompositions();
            await LoadOverlays();
            SelectedComposition = composition;
            StatusMessage = $"Scene created from template '{template.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scene from template");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddWidget(Widget? widget)
    {
        if (widget == null || SelectedComposition == null || _compositionService == null || _overlayService == null) return;

        try
        {
            // Map widget type to overlay type
            var overlayType = widget.Type switch
            {
                WidgetType.DateTime => OverlayType.DateTime,
                WidgetType.Clock => OverlayType.DateTime,
                WidgetType.Weather => OverlayType.Weather,
                WidgetType.Ticker => OverlayType.Ticker,
                WidgetType.Logo => OverlayType.Logo,
                WidgetType.QrCode => OverlayType.QrCode,
                _ => OverlayType.Text
            };

            // Create the overlay
            var overlay = new Overlay
            {
                Name = widget.Name,
                Type = overlayType,
                Content = widget.DefaultSettings.GetValueOrDefault("format", widget.Name),
                Position = new OverlayPosition
                {
                    Width = widget.DefaultWidth,
                    Height = widget.DefaultHeight
                }
            };

            await _overlayService.CreateOverlayAsync(overlay);

            // Add to composition at center
            var compositionOverlay = new CompositionOverlay
            {
                OverlayId = overlay.Id,
                Name = widget.Name,
                X = (SelectedComposition.Resolution.Width - widget.DefaultWidth) / 2,
                Y = (SelectedComposition.Resolution.Height - widget.DefaultHeight) / 2,
                Width = widget.DefaultWidth,
                Height = widget.DefaultHeight
            };

            await _compositionService.AddOverlayToCompositionAsync(SelectedComposition.Id, compositionOverlay);
            await LoadOverlays();
            OnPropertyChanged(nameof(SelectedComposition));
            StatusMessage = $"Widget '{widget.Name}' added to scene";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding widget");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void TogglePreviewMode()
    {
        IsPreviewMode = !IsPreviewMode;
        StatusMessage = IsPreviewMode ? "Preview mode enabled" : "Edit mode enabled";
    }

    [RelayCommand]
    private void CenterOverlay()
    {
        if (SelectedCompositionOverlay == null || SelectedComposition == null) return;
        
        SelectedCompositionOverlay.X = (SelectedComposition.Resolution.Width - SelectedCompositionOverlay.Width) / 2;
        SelectedCompositionOverlay.Y = (SelectedComposition.Resolution.Height - SelectedCompositionOverlay.Height) / 2;
        OnPropertyChanged(nameof(SelectedComposition));
        StatusMessage = "Overlay centered";
    }

    [RelayCommand]
    private void AlignOverlayLeft()
    {
        if (SelectedCompositionOverlay == null) return;
        
        SelectedCompositionOverlay.X = ALIGNMENT_MARGIN;
        OnPropertyChanged(nameof(SelectedComposition));
    }

    [RelayCommand]
    private void AlignOverlayRight()
    {
        if (SelectedCompositionOverlay == null || SelectedComposition == null) return;
        
        SelectedCompositionOverlay.X = SelectedComposition.Resolution.Width - SelectedCompositionOverlay.Width - ALIGNMENT_MARGIN;
        OnPropertyChanged(nameof(SelectedComposition));
    }

    [RelayCommand]
    private void AlignOverlayTop()
    {
        if (SelectedCompositionOverlay == null) return;
        
        SelectedCompositionOverlay.Y = ALIGNMENT_MARGIN;
        OnPropertyChanged(nameof(SelectedComposition));
    }

    [RelayCommand]
    private void AlignOverlayBottom()
    {
        if (SelectedCompositionOverlay == null || SelectedComposition == null) return;
        
        SelectedCompositionOverlay.Y = SelectedComposition.Resolution.Height - SelectedCompositionOverlay.Height - ALIGNMENT_MARGIN;
        OnPropertyChanged(nameof(SelectedComposition));
    }

    private void LoadSceneTemplates()
    {
        SceneTemplates.Clear();
        foreach (var template in DefaultSceneTemplates.GetTemplates())
        {
            SceneTemplates.Add(template);
        }
    }

    #region Zoom Commands

    private const double MIN_ZOOM = 0.1;
    private const double MAX_ZOOM = 2.0;
    private const double ZOOM_STEP = 0.1;
    private const double DEFAULT_ZOOM = 0.35;
    
    /// <summary>
    /// Estimated viewport width for zoom-to-fit calculations (in pixels)
    /// </summary>
    private const double VIEWPORT_WIDTH = 700.0;
    
    /// <summary>
    /// Estimated viewport height for zoom-to-fit calculations (in pixels)
    /// </summary>
    private const double VIEWPORT_HEIGHT = 500.0;

    /// <summary>
    /// Zooms in on the scene editor canvas
    /// </summary>
    [RelayCommand]
    private void ZoomIn()
    {
        if (ZoomLevel < MAX_ZOOM)
        {
            ZoomLevel = Math.Min(ZoomLevel + ZOOM_STEP, MAX_ZOOM);
            StatusMessage = $"Zoom: {ZoomLevel:P0}";
        }
    }

    /// <summary>
    /// Zooms out on the scene editor canvas
    /// </summary>
    [RelayCommand]
    private void ZoomOut()
    {
        if (ZoomLevel > MIN_ZOOM)
        {
            ZoomLevel = Math.Max(ZoomLevel - ZOOM_STEP, MIN_ZOOM);
            StatusMessage = $"Zoom: {ZoomLevel:P0}";
        }
    }

    /// <summary>
    /// Resets the zoom level to the default value
    /// </summary>
    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = DEFAULT_ZOOM;
        StatusMessage = $"Zoom reset to {ZoomLevel:P0}";
    }

    /// <summary>
    /// Sets the zoom level to fit the scene in the viewport
    /// </summary>
    [RelayCommand]
    private void ZoomToFit()
    {
        if (SelectedComposition != null)
        {
            var scaleX = VIEWPORT_WIDTH / SelectedComposition.Resolution.Width;
            var scaleY = VIEWPORT_HEIGHT / SelectedComposition.Resolution.Height;
            ZoomLevel = Math.Min(scaleX, scaleY);
            ZoomLevel = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, ZoomLevel));
            StatusMessage = $"Zoom to fit: {ZoomLevel:P0}";
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Asynchronously gets the local server URL by detecting the local IP address.
    /// Uses async DNS resolution to avoid blocking the UI thread.
    /// </summary>
    private async Task<string> GetLocalServerUrlAsync()
    {
        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var addresses = await System.Net.Dns.GetHostAddressesAsync(hostName);
            var localIp = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            
            if (localIp != null)
            {
                return $"ws://{localIp}:8443";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-detect local IP address");
        }
        
        return "ws://localhost:8443";
    }

    #endregion

    #endregion
}
