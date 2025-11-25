import Foundation

/// Represents the status of a signage client device
public enum ClientStatus: String, Codable, CaseIterable {
    case unknown = "Unknown"
    case online = "Online"
    case offline = "Offline"
    case installing = "Installing"
    case error = "Error"
    
    /// Returns the appropriate SF Symbol for this status
    public var symbolName: String {
        switch self {
        case .unknown:
            return "questionmark.circle"
        case .online:
            return "checkmark.circle.fill"
        case .offline:
            return "xmark.circle.fill"
        case .installing:
            return "arrow.down.circle"
        case .error:
            return "exclamationmark.triangle.fill"
        }
    }
    
    /// Returns the appropriate color for this status
    public var color: String {
        switch self {
        case .unknown:
            return "gray"
        case .online:
            return "green"
        case .offline:
            return "red"
        case .installing:
            return "blue"
        case .error:
            return "orange"
        }
    }
}

/// Represents a digital signage client device
/// Mirrors the .NET SignageClient model from MakerScreen.Core.Models
public struct SignageClient: Codable, Identifiable, Equatable {
    public let id: String
    public var name: String
    public var ipAddress: String
    public var macAddress: String
    public var status: ClientStatus
    public var lastSeen: Date
    public var version: String
    public var metadata: [String: String]
    
    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case name = "Name"
        case ipAddress = "IpAddress"
        case macAddress = "MacAddress"
        case status = "Status"
        case lastSeen = "LastSeen"
        case version = "Version"
        case metadata = "Metadata"
    }
    
    public init(
        id: String = UUID().uuidString,
        name: String = "",
        ipAddress: String = "",
        macAddress: String = "",
        status: ClientStatus = .unknown,
        lastSeen: Date = Date(),
        version: String = "",
        metadata: [String: String] = [:]
    ) {
        self.id = id
        self.name = name
        self.ipAddress = ipAddress
        self.macAddress = macAddress
        self.status = status
        self.lastSeen = lastSeen
        self.version = version
        self.metadata = metadata
    }
    
    public init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decode(String.self, forKey: .id)
        name = try container.decode(String.self, forKey: .name)
        ipAddress = try container.decode(String.self, forKey: .ipAddress)
        macAddress = try container.decode(String.self, forKey: .macAddress)
        
        // Handle status as either string or integer
        if let statusString = try? container.decode(String.self, forKey: .status) {
            status = ClientStatus(rawValue: statusString) ?? .unknown
        } else if let statusInt = try? container.decode(Int.self, forKey: .status) {
            let statuses: [ClientStatus] = [.unknown, .online, .offline, .installing, .error]
            status = statusInt < statuses.count ? statuses[statusInt] : .unknown
        } else {
            status = .unknown
        }
        
        // Parse date from ISO8601 string
        if let dateString = try? container.decode(String.self, forKey: .lastSeen) {
            let formatter = ISO8601DateFormatter()
            formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
            lastSeen = formatter.date(from: dateString) ?? Date()
        } else {
            lastSeen = Date()
        }
        
        version = try container.decode(String.self, forKey: .version)
        metadata = try container.decodeIfPresent([String: String].self, forKey: .metadata) ?? [:]
    }
    
    public func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(id, forKey: .id)
        try container.encode(name, forKey: .name)
        try container.encode(ipAddress, forKey: .ipAddress)
        try container.encode(macAddress, forKey: .macAddress)
        try container.encode(status.rawValue, forKey: .status)
        
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        try container.encode(formatter.string(from: lastSeen), forKey: .lastSeen)
        
        try container.encode(version, forKey: .version)
        try container.encode(metadata, forKey: .metadata)
    }
    
    /// Time elapsed since last seen
    public var timeSinceLastSeen: String {
        let interval = Date().timeIntervalSince(lastSeen)
        if interval < 60 {
            return "Just now"
        } else if interval < 3600 {
            let minutes = Int(interval / 60)
            return "\(minutes) min ago"
        } else if interval < 86400 {
            let hours = Int(interval / 3600)
            return "\(hours) hour\(hours == 1 ? "" : "s") ago"
        } else {
            let days = Int(interval / 86400)
            return "\(days) day\(days == 1 ? "" : "s") ago"
        }
    }
}
