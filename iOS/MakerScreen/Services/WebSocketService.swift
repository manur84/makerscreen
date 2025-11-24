import Foundation
import Combine

class WebSocketService: ObservableObject {
    @Published var isConnected = false
    @Published var clients: [ClientModel] = []
    
    private var webSocketTask: URLSessionWebSocketTask?
    private var cancellables = Set<AnyCancellable>()
    
    func connect(to url: String) {
        guard let url = URL(string: url) else { return }
        
        let session = URLSession(configuration: .default)
        webSocketTask = session.webSocketTask(with: url)
        webSocketTask?.resume()
        
        isConnected = true
        receiveMessage()
    }
    
    func disconnect() {
        webSocketTask?.cancel(with: .goingAway, reason: nil)
        isConnected = false
    }
    
    func send<T: Encodable>(_ message: T) {
        guard let data = try? JSONEncoder().encode(message),
              let string = String(data: data, encoding: .utf8) else {
            return
        }
        
        let message = URLSessionWebSocketTask.Message.string(string)
        webSocketTask?.send(message) { error in
            if let error = error {
                print("WebSocket send error: \(error)")
            }
        }
    }
    
    private func receiveMessage() {
        webSocketTask?.receive { [weak self] result in
            switch result {
            case .success(let message):
                switch message {
                case .string(let text):
                    self?.handleMessage(text)
                case .data(let data):
                    print("Received data: \(data)")
                @unknown default:
                    break
                }
                self?.receiveMessage()
                
            case .failure(let error):
                print("WebSocket receive error: \(error)")
                self?.isConnected = false
            }
        }
    }
    
    private func handleMessage(_ text: String) {
        print("Received message: \(text)")
        // TODO: Parse and handle different message types
    }
}

struct ClientModel: Identifiable, Codable {
    let id: Int
    let hostname: String
    let ipAddress: String?
    let status: String
    let lastSeen: Date?
}
