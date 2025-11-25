import Foundation

/// Connection state for the WebSocket service
public enum ConnectionState: Equatable {
    case disconnected
    case connecting
    case connected
    case reconnecting
    case error(String)
}

/// WebSocket message types matching the .NET MessageTypes constants
public struct MessageTypes {
    public static let register = "REGISTER"
    public static let heartbeat = "HEARTBEAT"
    public static let contentUpdate = "CONTENT_UPDATE"
    public static let command = "COMMAND"
    public static let installClient = "INSTALL_CLIENT"
    public static let status = "STATUS"
    public static let error = "ERROR"
    public static let clientList = "CLIENT_LIST"
    public static let contentList = "CONTENT_LIST"
}

/// WebSocket message protocol
/// Mirrors the .NET WebSocketMessage model from MakerScreen.Core.Models
public struct WebSocketMessage: Codable {
    public var type: String
    public var clientId: String
    public var data: MessageData?
    public var timestamp: Date
    
    enum CodingKeys: String, CodingKey {
        case type = "Type"
        case clientId = "ClientId"
        case data = "Data"
        case timestamp = "Timestamp"
    }
    
    public init(
        type: String,
        clientId: String = "",
        data: MessageData? = nil,
        timestamp: Date = Date()
    ) {
        self.type = type
        self.clientId = clientId
        self.data = data
        self.timestamp = timestamp
    }
    
    public init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        type = try container.decode(String.self, forKey: .type)
        clientId = try container.decode(String.self, forKey: .clientId)
        
        // Parse date from ISO8601 string
        if let dateString = try? container.decode(String.self, forKey: .timestamp) {
            let formatter = ISO8601DateFormatter()
            formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
            timestamp = formatter.date(from: dateString) ?? Date()
        } else {
            timestamp = Date()
        }
        
        // Try to decode data based on message type
        data = try? container.decode(MessageData.self, forKey: .data)
    }
    
    public func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(type, forKey: .type)
        try container.encode(clientId, forKey: .clientId)
        
        if let data = data {
            try container.encode(data, forKey: .data)
        }
        
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        try container.encode(formatter.string(from: timestamp), forKey: .timestamp)
    }
}

/// Wrapper for message data that can contain different types
public enum MessageData: Codable {
    case clients([SignageClient])
    case content([ContentItem])
    case client(SignageClient)
    case contentItem(ContentItem)
    case command(CommandData)
    case status(StatusData)
    case error(ErrorData)
    case raw(String)
    
    public init(from decoder: Decoder) throws {
        let container = try decoder.singleValueContainer()
        
        // Try to decode as array of clients
        if let clients = try? container.decode([SignageClient].self) {
            self = .clients(clients)
            return
        }
        
        // Try to decode as array of content items
        if let content = try? container.decode([ContentItem].self) {
            self = .content(content)
            return
        }
        
        // Try to decode as single client
        if let client = try? container.decode(SignageClient.self) {
            self = .client(client)
            return
        }
        
        // Try to decode as single content item
        if let contentItem = try? container.decode(ContentItem.self) {
            self = .contentItem(contentItem)
            return
        }
        
        // Try to decode as command data
        if let command = try? container.decode(CommandData.self) {
            self = .command(command)
            return
        }
        
        // Try to decode as status data
        if let status = try? container.decode(StatusData.self) {
            self = .status(status)
            return
        }
        
        // Try to decode as error data
        if let error = try? container.decode(ErrorData.self) {
            self = .error(error)
            return
        }
        
        // Fallback to raw string
        if let rawString = try? container.decode(String.self) {
            self = .raw(rawString)
            return
        }
        
        throw DecodingError.dataCorruptedError(in: container, debugDescription: "Unable to decode MessageData")
    }
    
    public func encode(to encoder: Encoder) throws {
        var container = encoder.singleValueContainer()
        switch self {
        case .clients(let clients):
            try container.encode(clients)
        case .content(let content):
            try container.encode(content)
        case .client(let client):
            try container.encode(client)
        case .contentItem(let contentItem):
            try container.encode(contentItem)
        case .command(let command):
            try container.encode(command)
        case .status(let status):
            try container.encode(status)
        case .error(let error):
            try container.encode(error)
        case .raw(let rawString):
            try container.encode(rawString)
        }
    }
}

/// Command data structure for client commands
public struct CommandData: Codable {
    public var action: String
    public var targetClientId: String?
    public var parameters: [String: String]?
    
    enum CodingKeys: String, CodingKey {
        case action = "Action"
        case targetClientId = "TargetClientId"
        case parameters = "Parameters"
    }
    
    public init(action: String, targetClientId: String? = nil, parameters: [String: String]? = nil) {
        self.action = action
        self.targetClientId = targetClientId
        self.parameters = parameters
    }
}

/// Status data structure for status updates
public struct StatusData: Codable {
    public var clientId: String
    public var status: ClientStatus
    public var message: String?
    
    enum CodingKeys: String, CodingKey {
        case clientId = "ClientId"
        case status = "Status"
        case message = "Message"
    }
    
    public init(clientId: String, status: ClientStatus, message: String? = nil) {
        self.clientId = clientId
        self.status = status
        self.message = message
    }
}

/// Error data structure for error messages
public struct ErrorData: Codable {
    public var code: String
    public var message: String
    public var details: String?
    
    enum CodingKeys: String, CodingKey {
        case code = "Code"
        case message = "Message"
        case details = "Details"
    }
    
    public init(code: String, message: String, details: String? = nil) {
        self.code = code
        self.message = message
        self.details = details
    }
}
