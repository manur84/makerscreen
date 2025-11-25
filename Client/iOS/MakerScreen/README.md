# MakerScreen iOS Management App

A SwiftUI-based iOS application for managing the MakerScreen Digital Signage System. This app connects to the MakerScreen server via WebSocket and provides real-time monitoring and control of signage clients.

## Features

- **Dashboard**: Overview of system status with statistics on connected clients and content
- **Client Management**: View all connected signage clients with status indicators (Online/Offline/Error/Installing)
- **Content Library**: Browse and manage content items in the system
- **Settings**: Configure server connection URL and app preferences
- **Real-time Updates**: WebSocket-based communication for live status updates

## Requirements

- **iOS 17.0+** or **macOS 14.0+**
- **Xcode 15.0+** (for building)
- Swift 5.9+

## Project Structure

```
MakerScreen/
├── Package.swift                 # Swift Package Manager manifest
├── Sources/
│   └── MakerScreen/
│       ├── MakerScreenApp.swift  # App entry point
│       ├── Models/
│       │   ├── SignageClient.swift      # Client device model
│       │   ├── ContentItem.swift        # Content item model
│       │   └── WebSocketMessage.swift   # WebSocket message models
│       ├── Services/
│       │   └── WebSocketService.swift   # WebSocket communication service
│       ├── Views/
│       │   ├── DashboardView.swift      # Main dashboard
│       │   ├── ClientListView.swift     # Client list and detail
│       │   ├── ContentLibraryView.swift # Content browser
│       │   └── SettingsView.swift       # App settings
│       └── ViewModels/
│           └── MakerScreenViewModel.swift # Main state management
└── Tests/
    └── MakerScreenTests/
        └── MakerScreenTests.swift        # Unit tests
```

## Building

### With Xcode (macOS only)

1. Open the `Package.swift` file in Xcode
2. Select your target device (iPhone, iPad, or Mac)
3. Build and run (⌘R)

### With Swift Package Manager (Linux/macOS)

The package can be built on Linux for validation (note: SwiftUI views won't compile on Linux as they require Apple frameworks):

```bash
cd Client/iOS/MakerScreen
swift build
swift test
```

## Data Models

The Swift models mirror the .NET server models for seamless JSON serialization:

### SignageClient
```swift
struct SignageClient {
    var id: String
    var name: String
    var ipAddress: String
    var macAddress: String
    var status: ClientStatus  // .unknown, .online, .offline, .installing, .error
    var lastSeen: Date
    var version: String
    var metadata: [String: String]
}
```

### ContentItem
```swift
struct ContentItem {
    var id: String
    var name: String
    var type: ContentType  // .image, .video, .html, .url
    var data: Data?
    var mimeType: String
    var createdAt: Date
    var duration: Int  // seconds
    var metadata: [String: String]
}
```

### WebSocketMessage
```swift
struct WebSocketMessage {
    var type: String  // REGISTER, HEARTBEAT, CONTENT_UPDATE, COMMAND, STATUS, ERROR
    var clientId: String
    var data: MessageData?
    var timestamp: Date
}
```

## Configuration

1. Launch the app
2. Go to Settings tab
3. Enter your MakerScreen server URL (e.g., `wss://yourserver:8443/ws`)
4. Tap "Save & Connect"

The app will automatically reconnect if the connection is lost.

## WebSocket Communication

The app communicates with the MakerScreen server using WebSocket Secure (WSS). Message types include:

- `REGISTER` - App registration with the server
- `HEARTBEAT` - Keep-alive messages
- `CLIENT_LIST` - Request/receive list of clients
- `CONTENT_LIST` - Request/receive content items
- `COMMAND` - Send commands to clients (restart, refresh content)
- `STATUS` - Receive status updates

## Architecture

The app follows the MVVM (Model-View-ViewModel) pattern:

- **Models**: Data structures that mirror the server models
- **Views**: SwiftUI views for the user interface
- **ViewModels**: State management and business logic
- **Services**: WebSocket communication layer

## License

This project is part of the MakerScreen Digital Signage System. See the main repository LICENSE file for details.
