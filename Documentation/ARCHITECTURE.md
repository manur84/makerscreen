# Architecture Documentation

## System Overview

MakerScreen is designed as a client-server architecture where a central Windows server manages multiple Raspberry Pi display clients. The system emphasizes zero-touch deployment, meaning clients can be automatically installed and configured without manual intervention.

## Core Components

### 1. Server Application (.NET 8)

#### MakerScreen.Core
Contains shared models, interfaces, and business logic.

**Key Components**:
- `SignageClient`: Model representing a connected display device
- `ContentItem`: Model for displayable content
- `WebSocketMessage`: Protocol for client-server communication
- `DeploymentPackage`: Deployment package metadata

**Interfaces**:
- `IWebSocketServer`: WebSocket server contract
- `IContentService`: Content management operations
- `IClientDeploymentService`: Client deployment operations

#### MakerScreen.Services

**SecureWebSocketServer**:
- Handles WebSocket connections on port 8443
- Manages client registration and authentication
- Routes messages between server and clients
- Implements heartbeat monitoring
- Supports concurrent client connections

**ClientDeploymentService**:
- Creates deployment packages (ZIP files with client code)
- Discovers Raspberry Pi devices on network
- Automates SSH-based deployment
- Generates bootable Raspberry Pi images
- Configures clients with server connection details

**ContentService**:
- Stores content on disk with metadata in memory
- Supports images, videos, HTML content
- Pushes content to clients via WebSocket
- Handles content versioning and updates

#### MakerScreen.Management (WPF)

**MVVM Architecture**:
- `MainViewModel`: Central view model with commands for all operations
- `MainWindow`: WPF UI with modern, card-based design
- Uses `CommunityToolkit.Mvvm` for MVVM pattern
- Dependency injection for service access

**Features**:
- Real-time client monitoring
- One-click deployment
- Content management with drag-drop
- Status monitoring and logging

### 2. Client Application (Python)

#### MakerScreenClient Class

**Connection Management**:
- Automatic connection to server
- WebSocket client using `websockets` library
- Exponential backoff for reconnection
- Heartbeat every 30 seconds

**Message Handling**:
- Registration on first connect
- Content update handling
- Command execution (reboot, update, clear)
- Status reporting

**Content Display**:
- Receives base64-encoded content
- Saves to local content directory
- Displays using appropriate renderer
- Supports rotation and scheduling (future)

**Service Integration**:
- Runs as systemd service
- Auto-starts on boot
- Logs to journald
- Automatic restart on failure

## Communication Protocol

### WebSocket Protocol

All messages use JSON format over WebSocket:

```json
{
  "type": "MESSAGE_TYPE",
  "clientId": "unique-client-id",
  "data": { /* type-specific data */ },
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### Message Types

#### Client to Server

**REGISTER**:
```json
{
  "type": "REGISTER",
  "clientId": "b827eb123456",
  "data": {
    "name": "Display-01",
    "macAddress": "b8:27:eb:12:34:56",
    "version": "1.0.0",
    "platform": "Linux"
  }
}
```

**HEARTBEAT**:
```json
{
  "type": "HEARTBEAT",
  "clientId": "b827eb123456",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

**STATUS**:
```json
{
  "type": "STATUS",
  "clientId": "b827eb123456",
  "data": {
    "status": "content_received",
    "contentId": "content-uuid"
  }
}
```

#### Server to Client

**CONTENT_UPDATE**:
```json
{
  "type": "CONTENT_UPDATE",
  "data": {
    "contentId": "uuid",
    "name": "welcome.png",
    "type": "Image",
    "mimeType": "image/png",
    "duration": 10,
    "data": "base64encoded..."
  }
}
```

**COMMAND**:
```json
{
  "type": "COMMAND",
  "data": {
    "command": "reboot"
  }
}
```

## Deployment Architecture

### Zero-Touch Deployment Flow

```
1. Server scans network for Raspberry Pi devices
   │
   ├─> Discovers device via mDNS/ARP
   │
   ├─> Verifies MAC address (Raspberry Pi prefix)
   │
   └─> Adds to deployment queue

2. For each device:
   │
   ├─> Creates deployment package
   │   ├─> Python client code
   │   ├─> Requirements.txt
   │   ├─> Systemd service file
   │   ├─> Installation script
   │   └─> Configuration with server URL
   │
   ├─> Transfers package via SSH/SCP
   │
   ├─> Executes installation script
   │   ├─> Creates /opt/makerscreen directory
   │   ├─> Installs Python dependencies
   │   ├─> Configures systemd service
   │   ├─> Enables auto-start
   │   └─> Starts service
   │
   └─> Client connects to server automatically
```

### Image-Based Deployment

```
1. Build custom Raspberry Pi OS image
   │
   ├─> Start with official Raspberry Pi OS Lite
   │
   ├─> Mount image partitions
   │
   ├─> Inject client files
   │   ├─> /opt/makerscreen/client.py
   │   ├─> /opt/makerscreen/config.json
   │   ├─> /etc/systemd/system/makerscreen.service
   │   └─> /etc/rc.local (first-boot setup)
   │
   ├─> Configure first-boot script
   │   ├─> Install dependencies
   │   ├─> Enable service
   │   └─> Mark setup complete
   │
   ├─> Compress image
   │
   └─> Flash to SD cards

2. Boot Raspberry Pi
   │
   ├─> First boot runs setup script
   │
   ├─> Client service starts automatically
   │
   └─> Connects to server
```

## Security Architecture

### Transport Security

**Development**:
- WebSocket (WS) on port 8443
- No encryption (local network only)

**Production**:
- WebSocket Secure (WSS) on port 8443
- TLS 1.2+ encryption
- Certificate-based authentication

### Authentication

**Client Authentication**:
- MAC address as unique identifier
- Optional pre-shared key
- IP whitelisting support

**Server Authentication**:
- Certificate validation (WSS)
- Challenge-response authentication (future)

### Network Isolation

```
Internet
   │
   ├─> Firewall
       │
       ├─> Management Network (VLAN 10)
       │   └─> Server (192.168.10.10)
       │
       └─> Signage Network (VLAN 20)
           ├─> Display 1 (192.168.20.11)
           ├─> Display 2 (192.168.20.12)
           └─> Display N (192.168.20.1N)
```

**Recommendations**:
- Separate VLAN for signage network
- Firewall rules limiting server access
- No internet access for displays
- Local content caching

## Data Flow

### Content Distribution

```
1. User uploads content via Management Console
   │
   ├─> File read into memory
   │
   ├─> ContentService.AddContentAsync()
   │   ├─> Save to disk
   │   ├─> Store metadata
   │   └─> Return content ID
   │
   └─> User clicks "Push"

2. ContentService.PushContentToClientsAsync()
   │
   ├─> Load content from disk
   │
   ├─> Encode as base64
   │
   ├─> Create CONTENT_UPDATE message
   │
   └─> WebSocketServer.BroadcastMessageAsync()

3. Each client receives message
   │
   ├─> Decode base64 data
   │
   ├─> Save to /opt/makerscreen/content/
   │
   ├─> Display content
   │
   └─> Send STATUS acknowledgment
```

### Client Status Monitoring

```
Client sends HEARTBEAT every 30 seconds
   │
   ├─> Server receives message
   │
   ├─> Updates LastSeen timestamp
   │
   ├─> Sets Status = Online
   │
   └─> Management Console refreshes (every 5s)
       │
       └─> Displays updated client list

If no HEARTBEAT for 60 seconds:
   │
   ├─> Server marks client as Offline
   │
   └─> Management Console shows warning
```

## Scalability

### Current Architecture

- **Clients**: Up to 100 concurrent connections
- **Content**: Limited by disk space
- **Network**: 1 Gbps recommended for HD video

### Scaling Considerations

**100-500 Clients**:
- Add content caching layer
- Implement content distribution via HTTP
- Use multicast for video streaming

**500-1000 Clients**:
- Deploy multiple WebSocket servers
- Load balancer for client connections
- Distributed content storage (CDN)

**1000+ Clients**:
- Microservices architecture
- Message queue (RabbitMQ/Redis)
- Database backend (SQL Server)
- Kubernetes deployment

## Error Handling

### Client-Side

```python
try:
    await self.websocket.send(message)
except websockets.ConnectionClosed:
    # Reconnect automatically
    logger.warning('Connection lost, reconnecting...')
    await asyncio.sleep(5)
    await self.connect()
except Exception as e:
    # Log error and continue
    logger.error(f'Error: {e}')
```

### Server-Side

```csharp
try
{
    await SendMessageAsync(client, message);
}
catch (WebSocketException ex)
{
    _logger.LogError(ex, "Error sending to client");
    // Remove disconnected client
    _clients.TryRemove(clientId, out _);
}
```

## Performance Optimization

### Content Caching

- Store content on disk
- Load on-demand
- LRU cache for frequently accessed content

### Network Optimization

- Delta updates for content changes
- Compression for large files
- Batch updates during off-hours

### Resource Management

- Connection pooling
- Async I/O throughout
- Memory-efficient base64 encoding
- Dispose pattern for unmanaged resources

## Monitoring and Logging

### Server Logging

```csharp
_logger.LogInformation("Client {ClientId} connected from {IpAddress}", 
    clientId, ipAddress);
_logger.LogWarning("Deployment to {ClientIp} failed", clientIp);
_logger.LogError(ex, "Error processing message");
```

**Log Levels**:
- **Information**: Normal operations
- **Warning**: Recoverable errors
- **Error**: Failures requiring attention
- **Debug**: Development troubleshooting

### Client Logging

```python
logger.info('Connected successfully!')
logger.warning('Connection lost, reconnecting...')
logger.error(f'Error handling message: {e}')
```

**Log Storage**:
- Server: Console + File
- Client: journald (systemd)

### Metrics (Future)

- Client connection count
- Content distribution time
- Network bandwidth usage
- Error rates
- Uptime statistics

## Future Enhancements

### Planned Features

1. **Database Backend**
   - SQL Server for persistent storage
   - Entity Framework Core
   - Content versioning
   - Audit trails

2. **Advanced Content Management**
   - Playlists and scheduling
   - Multi-zone layouts
   - Interactive content
   - Dynamic data overlays

3. **Enhanced Security**
   - Mutual TLS authentication
   - Role-based access control
   - Encrypted content storage
   - Security audit logging

4. **Monitoring Dashboard**
   - Real-time analytics
   - Performance metrics
   - Alert notifications
   - Remote diagnostics

5. **iOS Management App**
   - SwiftUI interface
   - Push notifications
   - AR-based display identification
   - Offline capability

6. **API Layer**
   - RESTful API
   - Third-party integrations
   - Webhook support
   - GraphQL endpoint
