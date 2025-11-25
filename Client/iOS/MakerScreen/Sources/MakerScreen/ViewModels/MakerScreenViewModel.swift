#if canImport(Combine) && (canImport(UIKit) || canImport(AppKit))
import Foundation
import Combine

/// Main ViewModel for the MakerScreen iOS app
/// Manages app state and coordinates between views and services
public class MakerScreenViewModel: ObservableObject {
    // MARK: - Published Properties
    
    @Published public var clients: [SignageClient] = []
    @Published public var contentItems: [ContentItem] = []
    @Published public var connectionState: ConnectionState = .disconnected
    @Published public var lastError: String?
    @Published public var serverURL: String = ""
    @Published public var isLoading: Bool = false
    
    // MARK: - Private Properties
    
    private let webSocketService: WebSocketService
    private var cancellables = Set<AnyCancellable>()
    
    // UserDefaults keys
    private let serverURLKey = "MakerScreen.ServerURL"
    
    // MARK: - Computed Properties
    
    /// Number of online clients
    public var onlineClientCount: Int {
        clients.filter { $0.status == .online }.count
    }
    
    /// Number of offline clients
    public var offlineClientCount: Int {
        clients.filter { $0.status == .offline }.count
    }
    
    /// Number of clients with errors
    public var errorClientCount: Int {
        clients.filter { $0.status == .error }.count
    }
    
    /// Total number of clients
    public var totalClientCount: Int {
        clients.count
    }
    
    /// Whether connected to the server
    public var isConnected: Bool {
        connectionState == .connected
    }
    
    /// Connection status text
    public var connectionStatusText: String {
        switch connectionState {
        case .disconnected:
            return "Disconnected"
        case .connecting:
            return "Connecting..."
        case .connected:
            return "Connected"
        case .reconnecting:
            return "Reconnecting..."
        case .error(let message):
            return "Error: \(message)"
        }
    }
    
    // MARK: - Initialization
    
    public init(webSocketService: WebSocketService = WebSocketService()) {
        self.webSocketService = webSocketService
        loadSettings()
        setupBindings()
    }
    
    // MARK: - Public Methods
    
    /// Connect to the server using the configured URL
    public func connect() {
        guard !serverURL.isEmpty else {
            lastError = "Server URL is not configured"
            return
        }
        
        // Ensure URL has proper scheme
        var url = serverURL
        if !url.hasPrefix("ws://") && !url.hasPrefix("wss://") {
            url = "wss://\(url)"
        }
        
        // Add /ws path if not present
        if !url.hasSuffix("/ws") && !url.contains("/ws?") {
            url = url.hasSuffix("/") ? "\(url)ws" : "\(url)/ws"
        }
        
        webSocketService.connect(to: url)
    }
    
    /// Disconnect from the server
    public func disconnect() {
        webSocketService.disconnect()
    }
    
    /// Refresh all data from the server
    public func refreshData() {
        isLoading = true
        webSocketService.refreshData()
        
        // Reset loading state after a delay
        DispatchQueue.main.asyncAfter(deadline: .now() + 2) { [weak self] in
            self?.isLoading = false
        }
    }
    
    /// Send a command to restart a client
    public func restartClient(_ clientId: String) {
        webSocketService.sendCommand("RESTART", to: clientId)
    }
    
    /// Send a command to refresh a client's content
    public func refreshClientContent(_ clientId: String) {
        webSocketService.sendCommand("REFRESH_CONTENT", to: clientId)
    }
    
    /// Send a command to display specific content on a client
    public func displayContent(_ contentId: String, on clientId: String) {
        webSocketService.sendCommand("DISPLAY_CONTENT", to: clientId, parameters: ["contentId": contentId])
    }
    
    /// Save settings to UserDefaults
    public func saveSettings() {
        UserDefaults.standard.set(serverURL, forKey: serverURLKey)
    }
    
    /// Load settings from UserDefaults
    public func loadSettings() {
        serverURL = UserDefaults.standard.string(forKey: serverURLKey) ?? ""
    }
    
    /// Update server URL and reconnect
    public func updateServerURL(_ url: String) {
        disconnect()
        serverURL = url
        saveSettings()
        if !url.isEmpty {
            connect()
        }
    }
    
    /// Get a client by ID
    public func client(withId id: String) -> SignageClient? {
        clients.first { $0.id == id }
    }
    
    /// Get content item by ID
    public func contentItem(withId id: String) -> ContentItem? {
        contentItems.first { $0.id == id }
    }
    
    /// Filter clients by status
    public func clients(withStatus status: ClientStatus) -> [SignageClient] {
        clients.filter { $0.status == status }
    }
    
    /// Filter content by type
    public func contentItems(ofType type: ContentType) -> [ContentItem] {
        contentItems.filter { $0.type == type }
    }
    
    // MARK: - Private Methods
    
    private func setupBindings() {
        // Bind WebSocket service properties to ViewModel
        webSocketService.$clients
            .receive(on: DispatchQueue.main)
            .assign(to: &$clients)
        
        webSocketService.$contentItems
            .receive(on: DispatchQueue.main)
            .assign(to: &$contentItems)
        
        webSocketService.$connectionState
            .receive(on: DispatchQueue.main)
            .assign(to: &$connectionState)
        
        webSocketService.$lastError
            .receive(on: DispatchQueue.main)
            .assign(to: &$lastError)
    }
}
#endif  // canImport(Combine) && (canImport(UIKit) || canImport(AppKit))
