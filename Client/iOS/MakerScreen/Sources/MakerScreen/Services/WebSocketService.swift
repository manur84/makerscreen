#if canImport(UIKit) || canImport(AppKit)
import Foundation
#if canImport(Combine)
import Combine
#endif

/// WebSocket service for communicating with the MakerScreen server
public class WebSocketService: NSObject, ObservableObject {
    // MARK: - Published Properties
    
    @Published public var connectionState: ConnectionState = .disconnected
    @Published public var clients: [SignageClient] = []
    @Published public var contentItems: [ContentItem] = []
    @Published public var lastError: String?
    
    // MARK: - Private Properties
    
    private var webSocket: URLSessionWebSocketTask?
    private var session: URLSession?
    private var serverURL: URL?
    private var reconnectTimer: Timer?
    private var heartbeatTimer: Timer?
    private let reconnectInterval: TimeInterval = 5.0
    private let heartbeatInterval: TimeInterval = 30.0
    private let clientId: String
    
    // MARK: - Initialization
    
    public override init() {
        self.clientId = "ios-app-\(UUID().uuidString.prefix(8))"
        super.init()
    }
    
    // MARK: - Public Methods
    
    /// Connect to the WebSocket server
    /// - Parameter urlString: The WebSocket URL (e.g., "wss://server:8443/ws")
    public func connect(to urlString: String) {
        guard let url = URL(string: urlString) else {
            connectionState = .error("Invalid URL")
            lastError = "Invalid server URL: \(urlString)"
            return
        }
        
        serverURL = url
        connectionState = .connecting
        
        // Create URL session with custom configuration
        let configuration = URLSessionConfiguration.default
        configuration.waitsForConnectivity = true
        configuration.timeoutIntervalForRequest = 30
        configuration.timeoutIntervalForResource = 60
        
        session = URLSession(configuration: configuration, delegate: self, delegateQueue: .main)
        webSocket = session?.webSocketTask(with: url)
        
        webSocket?.resume()
        startReceiving()
        
        // Send registration message
        sendRegistration()
    }
    
    /// Disconnect from the WebSocket server
    public func disconnect() {
        stopHeartbeat()
        stopReconnectTimer()
        
        webSocket?.cancel(with: .normalClosure, reason: nil)
        webSocket = nil
        session?.invalidateAndCancel()
        session = nil
        
        connectionState = .disconnected
    }
    
    /// Send a command to a specific client
    public func sendCommand(_ action: String, to targetClientId: String, parameters: [String: String]? = nil) {
        let commandData = CommandData(action: action, targetClientId: targetClientId, parameters: parameters)
        let message = WebSocketMessage(
            type: MessageTypes.command,
            clientId: clientId,
            data: .command(commandData)
        )
        send(message)
    }
    
    /// Request the list of clients from the server
    public func requestClientList() {
        let message = WebSocketMessage(
            type: MessageTypes.clientList,
            clientId: clientId
        )
        send(message)
    }
    
    /// Request the content list from the server
    public func requestContentList() {
        let message = WebSocketMessage(
            type: MessageTypes.contentList,
            clientId: clientId
        )
        send(message)
    }
    
    /// Refresh all data from the server
    public func refreshData() {
        requestClientList()
        requestContentList()
    }
    
    // MARK: - Private Methods
    
    private func sendRegistration() {
        let message = WebSocketMessage(
            type: MessageTypes.register,
            clientId: clientId,
            data: .raw("iOS Management App")
        )
        send(message)
    }
    
    private func send(_ message: WebSocketMessage) {
        guard connectionState == .connected || connectionState == .connecting else {
            return
        }
        
        do {
            let encoder = JSONEncoder()
            let data = try encoder.encode(message)
            guard let jsonString = String(data: data, encoding: .utf8) else { return }
            
            webSocket?.send(.string(jsonString)) { [weak self] error in
                if let error = error {
                    print("WebSocket send error: \(error)")
                    self?.handleConnectionError(error)
                }
            }
        } catch {
            print("Failed to encode message: \(error)")
        }
    }
    
    private func startReceiving() {
        webSocket?.receive { [weak self] result in
            switch result {
            case .success(let message):
                self?.handleMessage(message)
                // Continue receiving
                self?.startReceiving()
                
            case .failure(let error):
                print("WebSocket receive error: \(error)")
                self?.handleConnectionError(error)
            }
        }
    }
    
    private func handleMessage(_ message: URLSessionWebSocketTask.Message) {
        switch message {
        case .string(let text):
            processMessage(text)
        case .data(let data):
            if let text = String(data: data, encoding: .utf8) {
                processMessage(text)
            }
        @unknown default:
            break
        }
    }
    
    private func processMessage(_ jsonString: String) {
        guard let data = jsonString.data(using: .utf8) else { return }
        
        do {
            let decoder = JSONDecoder()
            let message = try decoder.decode(WebSocketMessage.self, from: data)
            
            DispatchQueue.main.async { [weak self] in
                self?.handleDecodedMessage(message)
            }
        } catch {
            print("Failed to decode message: \(error)")
            print("Raw message: \(jsonString)")
        }
    }
    
    private func handleDecodedMessage(_ message: WebSocketMessage) {
        switch message.type {
        case MessageTypes.clientList:
            if case .clients(let clientList) = message.data {
                self.clients = clientList
            }
            
        case MessageTypes.contentList:
            if case .content(let contentList) = message.data {
                self.contentItems = contentList
            }
            
        case MessageTypes.status:
            if case .status(let statusData) = message.data {
                updateClientStatus(clientId: statusData.clientId, status: statusData.status)
            } else if case .client(let client) = message.data {
                updateClient(client)
            }
            
        case MessageTypes.contentUpdate:
            if case .contentItem(let item) = message.data {
                updateContentItem(item)
            }
            
        case MessageTypes.heartbeat:
            // Server acknowledged heartbeat
            break
            
        case MessageTypes.error:
            if case .error(let errorData) = message.data {
                lastError = errorData.message
            }
            
        default:
            print("Unhandled message type: \(message.type)")
        }
    }
    
    private func updateClientStatus(clientId: String, status: ClientStatus) {
        if let index = clients.firstIndex(where: { $0.id == clientId }) {
            clients[index].status = status
            clients[index].lastSeen = Date()
        }
    }
    
    private func updateClient(_ client: SignageClient) {
        if let index = clients.firstIndex(where: { $0.id == client.id }) {
            clients[index] = client
        } else {
            clients.append(client)
        }
    }
    
    private func updateContentItem(_ item: ContentItem) {
        if let index = contentItems.firstIndex(where: { $0.id == item.id }) {
            contentItems[index] = item
        } else {
            contentItems.append(item)
        }
    }
    
    private func handleConnectionError(_ error: Error) {
        DispatchQueue.main.async { [weak self] in
            self?.connectionState = .error(error.localizedDescription)
            self?.lastError = error.localizedDescription
            self?.stopHeartbeat()
            self?.scheduleReconnect()
        }
    }
    
    private func startHeartbeat() {
        stopHeartbeat()
        heartbeatTimer = Timer.scheduledTimer(withTimeInterval: heartbeatInterval, repeats: true) { [weak self] _ in
            self?.sendHeartbeat()
        }
    }
    
    private func stopHeartbeat() {
        heartbeatTimer?.invalidate()
        heartbeatTimer = nil
    }
    
    private func sendHeartbeat() {
        let message = WebSocketMessage(
            type: MessageTypes.heartbeat,
            clientId: clientId
        )
        send(message)
    }
    
    private func scheduleReconnect() {
        guard connectionState != .connected else { return }
        
        stopReconnectTimer()
        connectionState = .reconnecting
        
        reconnectTimer = Timer.scheduledTimer(withTimeInterval: reconnectInterval, repeats: false) { [weak self] _ in
            guard let self = self, let url = self.serverURL else { return }
            self.connect(to: url.absoluteString)
        }
    }
    
    private func stopReconnectTimer() {
        reconnectTimer?.invalidate()
        reconnectTimer = nil
    }
}

// MARK: - URLSessionWebSocketDelegate

extension WebSocketService: URLSessionWebSocketDelegate {
    public func urlSession(_ session: URLSession, webSocketTask: URLSessionWebSocketTask, didOpenWithProtocol protocol: String?) {
        DispatchQueue.main.async { [weak self] in
            self?.connectionState = .connected
            self?.lastError = nil
            self?.startHeartbeat()
            self?.refreshData()
        }
    }
    
    public func urlSession(_ session: URLSession, webSocketTask: URLSessionWebSocketTask, didCloseWith closeCode: URLSessionWebSocketTask.CloseCode, reason: Data?) {
        DispatchQueue.main.async { [weak self] in
            self?.connectionState = .disconnected
            self?.stopHeartbeat()
            
            // Attempt to reconnect if not intentionally closed
            if closeCode != .normalClosure {
                self?.scheduleReconnect()
            }
        }
    }
    
    public func urlSession(_ session: URLSession, task: URLSessionTask, didCompleteWithError error: Error?) {
        if let error = error {
            handleConnectionError(error)
        }
    }
}
#endif  // canImport(UIKit) || canImport(AppKit)
