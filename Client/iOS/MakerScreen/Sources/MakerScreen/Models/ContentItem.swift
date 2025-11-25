import Foundation

/// Represents the type of content that can be displayed
public enum ContentType: String, Codable, CaseIterable {
    case image = "Image"
    case video = "Video"
    case html = "Html"
    case url = "Url"
    
    /// Returns the appropriate SF Symbol for this content type
    public var symbolName: String {
        switch self {
        case .image:
            return "photo"
        case .video:
            return "play.rectangle"
        case .html:
            return "doc.richtext"
        case .url:
            return "globe"
        }
    }
}

/// Represents content to be displayed on signage clients
/// Mirrors the .NET ContentItem model from MakerScreen.Core.Models
public struct ContentItem: Codable, Identifiable, Equatable {
    public let id: String
    public var name: String
    public var type: ContentType
    public var data: Data?
    public var mimeType: String
    public var createdAt: Date
    public var duration: Int  // seconds
    public var metadata: [String: String]
    
    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case name = "Name"
        case type = "Type"
        case data = "Data"
        case mimeType = "MimeType"
        case createdAt = "CreatedAt"
        case duration = "Duration"
        case metadata = "Metadata"
    }
    
    public init(
        id: String = UUID().uuidString,
        name: String = "",
        type: ContentType = .image,
        data: Data? = nil,
        mimeType: String = "",
        createdAt: Date = Date(),
        duration: Int = 10,
        metadata: [String: String] = [:]
    ) {
        self.id = id
        self.name = name
        self.type = type
        self.data = data
        self.mimeType = mimeType
        self.createdAt = createdAt
        self.duration = duration
        self.metadata = metadata
    }
    
    public init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decode(String.self, forKey: .id)
        name = try container.decode(String.self, forKey: .name)
        
        // Handle type as either string or integer
        if let typeString = try? container.decode(String.self, forKey: .type) {
            type = ContentType(rawValue: typeString) ?? .image
        } else if let typeInt = try? container.decode(Int.self, forKey: .type) {
            let types: [ContentType] = [.image, .video, .html, .url]
            type = typeInt < types.count ? types[typeInt] : .image
        } else {
            type = .image
        }
        
        // Handle base64-encoded data
        if let base64String = try? container.decode(String.self, forKey: .data) {
            data = Data(base64Encoded: base64String)
        } else {
            data = nil
        }
        
        mimeType = try container.decode(String.self, forKey: .mimeType)
        
        // Parse date from ISO8601 string
        if let dateString = try? container.decode(String.self, forKey: .createdAt) {
            let formatter = ISO8601DateFormatter()
            formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
            createdAt = formatter.date(from: dateString) ?? Date()
        } else {
            createdAt = Date()
        }
        
        duration = try container.decode(Int.self, forKey: .duration)
        metadata = try container.decodeIfPresent([String: String].self, forKey: .metadata) ?? [:]
    }
    
    public func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(id, forKey: .id)
        try container.encode(name, forKey: .name)
        try container.encode(type.rawValue, forKey: .type)
        
        if let data = data {
            try container.encode(data.base64EncodedString(), forKey: .data)
        }
        
        try container.encode(mimeType, forKey: .mimeType)
        
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        try container.encode(formatter.string(from: createdAt), forKey: .createdAt)
        
        try container.encode(duration, forKey: .duration)
        try container.encode(metadata, forKey: .metadata)
    }
    
    /// Formatted duration string
    public var formattedDuration: String {
        if duration < 60 {
            return "\(duration)s"
        } else {
            let minutes = duration / 60
            let seconds = duration % 60
            return seconds > 0 ? "\(minutes)m \(seconds)s" : "\(minutes)m"
        }
    }
    
    /// Formatted creation date
    public var formattedCreatedAt: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: createdAt)
    }
}
