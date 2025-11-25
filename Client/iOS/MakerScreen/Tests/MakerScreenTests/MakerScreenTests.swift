import XCTest
@testable import MakerScreen

final class MakerScreenTests: XCTestCase {
    
    // MARK: - SignageClient Tests
    
    func testSignageClientInitialization() {
        let client = SignageClient(
            id: "test-id",
            name: "Test Client",
            ipAddress: "192.168.1.100",
            macAddress: "AA:BB:CC:DD:EE:FF",
            status: .online,
            version: "1.0.0"
        )
        
        XCTAssertEqual(client.id, "test-id")
        XCTAssertEqual(client.name, "Test Client")
        XCTAssertEqual(client.ipAddress, "192.168.1.100")
        XCTAssertEqual(client.macAddress, "AA:BB:CC:DD:EE:FF")
        XCTAssertEqual(client.status, .online)
        XCTAssertEqual(client.version, "1.0.0")
    }
    
    func testSignageClientJSONDecoding() throws {
        let json = """
        {
            "Id": "client-123",
            "Name": "Display 1",
            "IpAddress": "10.0.0.50",
            "MacAddress": "11:22:33:44:55:66",
            "Status": "Online",
            "LastSeen": "2024-01-15T10:30:00.000Z",
            "Version": "2.0.0",
            "Metadata": {
                "location": "Lobby",
                "floor": "1"
            }
        }
        """.data(using: .utf8)!
        
        let client = try JSONDecoder().decode(SignageClient.self, from: json)
        
        XCTAssertEqual(client.id, "client-123")
        XCTAssertEqual(client.name, "Display 1")
        XCTAssertEqual(client.ipAddress, "10.0.0.50")
        XCTAssertEqual(client.status, .online)
        XCTAssertEqual(client.version, "2.0.0")
        XCTAssertEqual(client.metadata["location"], "Lobby")
    }
    
    func testSignageClientStatusDecoding() throws {
        // Test integer status decoding
        let jsonWithIntStatus = """
        {
            "Id": "client-1",
            "Name": "Test",
            "IpAddress": "10.0.0.1",
            "MacAddress": "",
            "Status": 1,
            "LastSeen": "2024-01-15T10:30:00.000Z",
            "Version": "1.0",
            "Metadata": {}
        }
        """.data(using: .utf8)!
        
        let client = try JSONDecoder().decode(SignageClient.self, from: jsonWithIntStatus)
        XCTAssertEqual(client.status, .online)
    }
    
    func testClientStatusProperties() {
        XCTAssertEqual(ClientStatus.online.rawValue, "Online")
        XCTAssertEqual(ClientStatus.offline.rawValue, "Offline")
        XCTAssertEqual(ClientStatus.error.rawValue, "Error")
        XCTAssertEqual(ClientStatus.installing.rawValue, "Installing")
        XCTAssertEqual(ClientStatus.unknown.rawValue, "Unknown")
        
        // Test symbol names
        XCTAssertEqual(ClientStatus.online.symbolName, "checkmark.circle.fill")
        XCTAssertEqual(ClientStatus.offline.symbolName, "xmark.circle.fill")
    }
    
    // MARK: - ContentItem Tests
    
    func testContentItemInitialization() {
        let content = ContentItem(
            id: "content-1",
            name: "Welcome Image",
            type: .image,
            mimeType: "image/png",
            duration: 30
        )
        
        XCTAssertEqual(content.id, "content-1")
        XCTAssertEqual(content.name, "Welcome Image")
        XCTAssertEqual(content.type, .image)
        XCTAssertEqual(content.mimeType, "image/png")
        XCTAssertEqual(content.duration, 30)
    }
    
    func testContentItemJSONDecoding() throws {
        let json = """
        {
            "Id": "content-456",
            "Name": "Promo Video",
            "Type": "Video",
            "MimeType": "video/mp4",
            "CreatedAt": "2024-01-10T08:00:00.000Z",
            "Duration": 120,
            "Metadata": {
                "resolution": "1920x1080"
            }
        }
        """.data(using: .utf8)!
        
        let content = try JSONDecoder().decode(ContentItem.self, from: json)
        
        XCTAssertEqual(content.id, "content-456")
        XCTAssertEqual(content.name, "Promo Video")
        XCTAssertEqual(content.type, .video)
        XCTAssertEqual(content.mimeType, "video/mp4")
        XCTAssertEqual(content.duration, 120)
    }
    
    func testContentTypeProperties() {
        XCTAssertEqual(ContentType.image.rawValue, "Image")
        XCTAssertEqual(ContentType.video.rawValue, "Video")
        XCTAssertEqual(ContentType.html.rawValue, "Html")
        XCTAssertEqual(ContentType.url.rawValue, "Url")
        
        // Test symbol names
        XCTAssertEqual(ContentType.image.symbolName, "photo")
        XCTAssertEqual(ContentType.video.symbolName, "play.rectangle")
    }
    
    func testFormattedDuration() {
        let shortContent = ContentItem(duration: 45)
        XCTAssertEqual(shortContent.formattedDuration, "45s")
        
        let mediumContent = ContentItem(duration: 90)
        XCTAssertEqual(mediumContent.formattedDuration, "1m 30s")
        
        let longContent = ContentItem(duration: 120)
        XCTAssertEqual(longContent.formattedDuration, "2m")
    }
    
    // MARK: - WebSocketMessage Tests
    
    func testWebSocketMessageInitialization() {
        let message = WebSocketMessage(
            type: MessageTypes.register,
            clientId: "ios-app-123"
        )
        
        XCTAssertEqual(message.type, "REGISTER")
        XCTAssertEqual(message.clientId, "ios-app-123")
    }
    
    func testWebSocketMessageJSONDecoding() throws {
        let json = """
        {
            "Type": "STATUS",
            "ClientId": "client-789",
            "Timestamp": "2024-01-15T12:00:00.000Z"
        }
        """.data(using: .utf8)!
        
        let message = try JSONDecoder().decode(WebSocketMessage.self, from: json)
        
        XCTAssertEqual(message.type, "STATUS")
        XCTAssertEqual(message.clientId, "client-789")
    }
    
    func testMessageTypesConstants() {
        XCTAssertEqual(MessageTypes.register, "REGISTER")
        XCTAssertEqual(MessageTypes.heartbeat, "HEARTBEAT")
        XCTAssertEqual(MessageTypes.contentUpdate, "CONTENT_UPDATE")
        XCTAssertEqual(MessageTypes.command, "COMMAND")
        XCTAssertEqual(MessageTypes.status, "STATUS")
        XCTAssertEqual(MessageTypes.error, "ERROR")
    }
    
    // MARK: - CommandData Tests
    
    func testCommandDataEncoding() throws {
        let command = CommandData(
            action: "RESTART",
            targetClientId: "client-1",
            parameters: ["delay": "5"]
        )
        
        let encoded = try JSONEncoder().encode(command)
        let decoded = try JSONDecoder().decode(CommandData.self, from: encoded)
        
        XCTAssertEqual(decoded.action, "RESTART")
        XCTAssertEqual(decoded.targetClientId, "client-1")
        XCTAssertEqual(decoded.parameters?["delay"], "5")
    }
    
    // MARK: - ConnectionState Tests
    
    func testConnectionStateEquality() {
        XCTAssertEqual(ConnectionState.connected, ConnectionState.connected)
        XCTAssertEqual(ConnectionState.disconnected, ConnectionState.disconnected)
        XCTAssertNotEqual(ConnectionState.connected, ConnectionState.disconnected)
        
        let error1 = ConnectionState.error("Test error")
        let error2 = ConnectionState.error("Test error")
        XCTAssertEqual(error1, error2)
    }
    
    // MARK: - JSON Encoding/Decoding Round Trip Tests
    
    func testSignageClientRoundTrip() throws {
        let original = SignageClient(
            id: "test-123",
            name: "Test Display",
            ipAddress: "192.168.1.50",
            macAddress: "AA:BB:CC:DD:EE:FF",
            status: .online,
            lastSeen: Date(),
            version: "1.2.3",
            metadata: ["room": "Conference A"]
        )
        
        let encoder = JSONEncoder()
        let data = try encoder.encode(original)
        
        let decoder = JSONDecoder()
        let decoded = try decoder.decode(SignageClient.self, from: data)
        
        XCTAssertEqual(decoded.id, original.id)
        XCTAssertEqual(decoded.name, original.name)
        XCTAssertEqual(decoded.status, original.status)
        XCTAssertEqual(decoded.metadata["room"], "Conference A")
    }
    
    func testContentItemRoundTrip() throws {
        let original = ContentItem(
            id: "content-test",
            name: "Test Content",
            type: .html,
            mimeType: "text/html",
            duration: 60,
            metadata: ["author": "Test User"]
        )
        
        let encoder = JSONEncoder()
        let data = try encoder.encode(original)
        
        let decoder = JSONDecoder()
        let decoded = try decoder.decode(ContentItem.self, from: data)
        
        XCTAssertEqual(decoded.id, original.id)
        XCTAssertEqual(decoded.name, original.name)
        XCTAssertEqual(decoded.type, original.type)
        XCTAssertEqual(decoded.duration, original.duration)
    }
}
