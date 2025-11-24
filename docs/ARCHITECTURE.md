# MakerScreen System Architecture

## Table of Contents
1. [Overview](#overview)
2. [System Components](#system-components)
3. [Security Architecture](#security-architecture)
4. [Network Architecture](#network-architecture)
5. [Data Flow](#data-flow)
6. [Component Details](#component-details)
7. [Integration Points](#integration-points)

## Overview

MakerScreen is a highly secure, enterprise-grade Digital Signage Management System designed for complete network isolation with zero-touch deployment capabilities. The system consists of three primary components working in concert:

- **Windows Server Application** (.NET 8 WPF)
- **Raspberry Pi Clients** (Python 3.11+)
- **iOS Management App** (SwiftUI)

### Design Principles

1. **Security First**: All communications encrypted, mutual TLS, no Internet exposure
2. **Zero Touch**: Minimal manual configuration required
3. **Self-Healing**: Automatic recovery from failures
4. **Scalability**: Support for 100+ displays
5. **User Experience**: Intuitive interfaces across all platforms

## System Components

### 1. Server Component (Windows/.NET 8)

#### Architecture Layers

```
┌─────────────────────────────────────────────────┐
│           WPF Management Application            │
│  ┌──────────────┐  ┌──────────────────────────┐ │
│  │  UI Layer    │  │   ViewModel Layer        │ │
│  │  (MVVM)      │  │   (Business Logic)       │ │
│  └──────────────┘  └──────────────────────────┘ │
└─────────────────────────────────────────────────┘
                     ↕
┌─────────────────────────────────────────────────┐
│              Service Layer                       │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────┐ │
│  │ WebSocket│ │ Image    │ │ Auto-Discovery  │ │
│  │ Server   │ │ Builder  │ │ Service         │ │
│  └──────────┘ └──────────┘ └─────────────────┘ │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────┐ │
│  │ Content  │ │ Overlay  │ │ Client          │ │
│  │ Manager  │ │ Engine   │ │ Manager         │ │
│  └──────────┘ └──────────┘ └─────────────────┘ │
└─────────────────────────────────────────────────┘
                     ↕
┌─────────────────────────────────────────────────┐
│              Data Layer                          │
│  ┌──────────────────────────────────────────┐   │
│  │  Entity Framework Core                   │   │
│  │  (Repository Pattern)                    │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
                     ↕
┌─────────────────────────────────────────────────┐
│           SQL Server Database                    │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────┐ │
│  │ Content  │ │ Clients  │ │ Audit Trail     │ │
│  │ (FILESTR)│ │ Registry │ │                 │ │
│  └──────────┘ └──────────┘ └─────────────────┘ │
└─────────────────────────────────────────────────┘
```

#### Key Features

**Zero-Touch Client Deployment**
- Integrated Raspberry Pi OS image builder
- Pre-configured network settings, SSH keys, certificates
- Embedded Python client with all dependencies
- Direct SD card writing from application
- PXE boot server for network deployment
- mDNS broadcasting for auto-discovery
- QR-code based pairing alternative

**Content Management**
- Drag & drop PNG import
- Hotfolder monitoring for automated import
- Git-based version control for layouts
- Automatic thumbnail generation (multiple sizes)
- Comprehensive metadata (creator, dates, tags, dimensions)
- Content deduplication using SHA256 hashing

**Dynamic Overlay System**
- Visual WYSIWYG designer with drag-and-drop rectangles
- Multi-layer support (up to 32 layers per layout)
- Data source configuration:
  - Multiple SQL Server connections
  - Stored procedures and views
  - REST API endpoints
  - Static data sources
- Overlay types:
  - Text with custom fonts, colors, animations
  - Scrolling ticker
  - Real-time charts (line, bar, pie)
  - Dynamic QR codes
  - Date/time with timezone support
  - Weather widgets
  - RSS feeds
- Template library with drag-and-drop instantiation
- Real-time preview with sample data

**Client Management**
- Auto-discovery via mDNS/Zeroconf
- Hierarchical grouping (locations, buildings, departments)
- Real-time heartbeat monitoring (configurable intervals)
- Alert system (email, SMS, push notifications)
- Remote diagnostics:
  - Live screenshot capture
  - Log streaming
  - System information
  - Performance metrics
- Bandwidth management with QoS
- Bulk operations (update, reboot, content assignment)
- Client capability detection

**Scheduling & Playlists**
- Calendar-based content planning
- Recurring schedules with exceptions
- Priority-based interruptions (emergency broadcasts)
- A/B testing with analytics
- Conditional display rules:
  - Time of day
  - Day of week
  - Weather conditions
  - Occupancy sensors
  - Custom SQL queries
- Playlist editor with transitions

**Plug & Play Server Setup**
- Single-file installer with embedded dependencies:
  - SQL Server Express (optional)
  - .NET 8 Runtime
  - All required libraries
- Automated configuration wizard:
  - Database setup (local or remote)
  - WebSocket server configuration
  - Firewall rules creation
  - Windows Service installation
  - SSL certificate generation
- Health check dashboard with real-time status
- One-click backup and restore

#### Technical Stack

- **Framework**: .NET 8.0
- **UI**: WPF with MVVM pattern
- **WebSocket**: System.Net.WebSockets with TLS 1.3
- **Database**: Entity Framework Core 8.0
- **Real-time**: SignalR for push updates
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog with multiple sinks
- **Image Processing**: ImageSharp
- **Security**: BouncyCastle for certificate management

### 2. Client Component (Raspberry Pi/Python)

#### Architecture

```
┌─────────────────────────────────────────────────┐
│         Display Application (PyQt5)              │
│  ┌──────────────┐  ┌──────────────────────────┐ │
│  │  Rendering   │  │   Overlay Engine         │ │
│  │  Engine      │  │   (GPU Accelerated)      │ │
│  └──────────────┘  └──────────────────────────┘ │
└─────────────────────────────────────────────────┘
                     ↕
┌─────────────────────────────────────────────────┐
│            Service Layer                         │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────┐ │
│  │ WebSocket│ │ Content  │ │ Status Screen   │ │
│  │ Client   │ │ Manager  │ │ Generator       │ │
│  └──────────┘ └──────────┘ └─────────────────┘ │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────┐ │
│  │ Discovery│ │ Watchdog │ │ Local Web UI    │ │
│  │ Service  │ │ Service  │ │ (Flask)         │ │
│  └──────────┘ └──────────┘ └─────────────────┘ │
└─────────────────────────────────────────────────┘
                     ↕
┌─────────────────────────────────────────────────┐
│         Hardware Abstraction Layer               │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────┐ │
│  │ Display  │ │ HDMI-CEC │ │ GPIO/Sensors    │ │
│  │ Control  │ │ Control  │ │                 │ │
│  └──────────┘ └──────────┘ └─────────────────┘ │
└─────────────────────────────────────────────────┘
```

#### Key Features

**Vollautomatisches Setup**
- First boot configuration:
  - Automatic server discovery (mDNS broadcast)
  - Self-registration with hardware info
  - Display auto-calibration
  - Performance tuning based on Pi model
- Boot to content in < 60 seconds
- No manual intervention required

**Status & Diagnostic Interface**

*Smart Status Screens* (when no layout/connection):
- Rotating displays (15-second intervals):
  - System information (hostname, IP, MAC, version)
  - Connection status with retry counter
  - Hardware details (CPU, RAM, temperature, GPU)
  - Last successful content timestamp
  - Network diagnostics results
- Large QR code (300x300px minimum) with URL to local web UI
- Color-coded status:
  - Green: Connected, content playing
  - Yellow: Connected, no content
  - Red: No server connection
  - Blue: Diagnostic mode
- Animated connection attempt indicators
- Self-refreshing every 30 seconds

*Local Web Server* (Flask-based, port 8080):
- Accessible via QR code scan
- Responsive design (mobile/tablet/desktop)
- Features:
  - Real-time system dashboard
  - Network configuration (WiFi/Ethernet)
  - Server connection settings
  - Display settings (resolution, rotation, brightness)
  - Log viewer with filtering and search
  - Manual content upload (emergency backup)
  - Diagnostic tools:
    - Ping server
    - Traceroute
    - Network speed test
    - Display test patterns
  - Reboot/shutdown controls
  - Debug mode toggle
  - Certificate management
- Auto-discovery broadcast: "Unconfigured client available"
- mDNS advertising: `makerscreen-client-{mac}.local`

**Display Management**
- Hardware acceleration using GPU (OpenGL ES)
- Multi-monitor support (up to 4 displays)
- Automatic display rotation (0°, 90°, 180°, 270°)
- HDMI-CEC for automatic display power control
- Fallback content on connection loss
- Status screen rotation when idle (> 5 minutes no content)

**Content Handling**
- Local cache with intelligent preloading
- Delta update system (binary diff for efficiency)
- Automatic image optimization:
  - Format conversion (WebP for efficiency)
  - Resolution matching to display
  - Compression without quality loss
- Smooth transitions (fade, slide, zoom)
- Fallback to cached content offline
- Cache size management (configurable, default 2GB)

**System Stability**
- Hardware watchdog timer (automatic reboot on hang)
- Process monitoring and auto-restart
- Memory management with automatic cleanup
- Network reconnection logic (exponential backoff)
- Offline mode with local content queue
- Automatic update system from server
- Self-healing mechanisms:
  - Corrupt cache detection and cleanup
  - Network interface reset on failure
  - Display reinitialisation on errors
  - Log rotation to prevent disk fill

#### Technical Stack

- **Language**: Python 3.11+
- **Display**: PyQt5 with OpenGL acceleration
- **Web UI**: Flask with responsive templates
- **WebSocket**: websockets library with async/await
- **Image**: Pillow with optimization plugins
- **Network**: Zeroconf for mDNS
- **System**: systemd for service management
- **Hardware**: RPi.GPIO, cec library for HDMI-CEC

### 3. iOS Management App (SwiftUI)

#### Architecture

```
┌─────────────────────────────────────────────────┐
│              SwiftUI Views                       │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────┐ │
│  │Dashboard │ │ Content  │ │ Client          │ │
│  │ View     │ │ Manager  │ │ Management      │ │
│  └──────────┘ └──────────┘ └─────────────────┘ │
└─────────────────────────────────────────────────┘
                     ↕
┌─────────────────────────────────────────────────┐
│          ViewModels (Combine)                    │
│  ┌──────────────────────────────────────────┐   │
│  │  ObservableObject Classes                │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
                     ↕
┌─────────────────────────────────────────────────┐
│            Services Layer                        │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────┐ │
│  │ WebSocket│ │ API      │ │ Notification    │ │
│  │ Manager  │ │ Client   │ │ Service         │ │
│  └──────────┘ └──────────┘ └─────────────────┘ │
└─────────────────────────────────────────────────┘
                     ↕
┌─────────────────────────────────────────────────┐
│         Core Data / Local Storage                │
└─────────────────────────────────────────────────┘
```

#### Key Features

**Core Management**
- Live dashboard with all clients overview
- Content management:
  - Upload images from Photos library
  - Camera capture integration
  - Layout preview and basic editing
  - Simplified overlay editor
- Client control:
  - Remote screenshot (push to server, retrieve)
  - Reboot/shutdown commands
  - Content assignment with preview
  - Group management with drag-and-drop
- Scheduling:
  - Full playlist management
  - Calendar view with drag-and-drop
  - Quick schedule templates
- QR scanner for direct client web UI access

**Mobile-Optimized Features**
- Push notifications:
  - Client offline alerts
  - Error notifications
  - Content update confirmations
  - System health warnings
- Quick Actions:
  - 3D Touch / Long press menus
  - Shortcuts to common tasks
- Home Screen Widgets:
  - Live status overview
  - Quick stats (online/offline counts)
  - Recent alerts
- Siri Shortcuts:
  - "Show all displays in Building A"
  - "Upload content to lobby screens"
  - "Check system status"
- Apple Watch Companion:
  - Basic monitoring
  - Alert triage
  - Quick client power control

**Advanced Features**
- AR View:
  - Point camera at display
  - Overlay client info (name, IP, status, current content)
  - Quick access to controls
- NFC Tags:
  - Tap tag on display for instant details
  - Pre-programmed actions
- Share Extension:
  - Share images from any app to MakerScreen
  - Automatic import and distribution
- Offline Mode:
  - Cached management functions
  - Queued commands (execute when online)
  - Local content preview
- Biometric Security:
  - Face ID / Touch ID for app launch
  - Required for sensitive operations
- Client Discovery:
  - Automatic detection of status screen clients
  - Network scanning
  - QR code scanning for quick add

**Integration**
- MDM Support:
  - Managed app configuration
  - Certificate distribution
  - App update policies
- SharePlay:
  - Collaborative content review
  - Multi-user editing sessions
- Handoff:
  - Seamless transition between iOS and desktop
  - Continue tasks across devices

#### Technical Stack

- **Language**: Swift 5.9+
- **UI**: SwiftUI
- **Reactive**: Combine framework
- **Networking**: URLSession, NWPathMonitor
- **WebSocket**: Starscream or native URLSessionWebSocketTask
- **Storage**: Core Data
- **AR**: ARKit
- **NFC**: Core NFC
- **Push**: Apple Push Notification Service (APNs)

## Security Architecture

### Network Isolation

```
┌────────────────────────────────────────────────┐
│         Corporate Network (Isolated)           │
│                                                │
│  ┌──────────────┐         ┌────────────────┐  │
│  │   Firewall   │────────▶│  MakerScreen   │  │
│  │   (Ingress)  │         │  Server        │  │
│  │   - Block    │         │  (WSS:8443)    │  │
│  │     Internet │         └────────────────┘  │
│  │   - Allow    │                ▲            │
│  │     Internal │                │            │
│  └──────────────┘                │ WSS/TLS1.3 │
│                                  │ Mutual TLS │
│  ┌─────────────────────────────┐│            │
│  │  Raspberry Pi Clients       ││            │
│  │  ┌────────┐  ┌────────┐    ││            │
│  │  │Client 1│  │Client 2│ ···││            │
│  │  └────────┘  └────────┘    ││            │
│  │  mDNS Discovery Enabled    ││            │
│  └─────────────────────────────┘│            │
│                                  │            │
│  ┌──────────────────────────────┘            │
│  │  iOS Devices (VPN/Local WiFi)             │
│  │  ┌──────────┐  ┌──────────┐               │
│  │  │ iPhone   │  │  iPad    │               │
│  │  └──────────┘  └──────────┘               │
│  └───────────────────────────────────────────│
└────────────────────────────────────────────────┘
         │
         │ Physical Air Gap
         ▼
┌────────────────────┐
│     Internet       │
│   (No Access)      │
└────────────────────┘
```

### Certificate Management

**Self-Signed Certificate Authority (CA)**

1. **Root CA** (offline storage)
   - Generate once during installation
   - Store in secure hardware (HSM recommended)
   - Used only to sign intermediate certificates

2. **Intermediate CA** (server)
   - Signs all client and server certificates
   - Automatic renewal before expiration
   - Revocation list maintained

3. **Server Certificate**
   - Subject: CN=makerscreen-server.local
   - Extended Key Usage: Server Authentication
   - Validity: 2 years
   - Auto-renewal at 90 days

4. **Client Certificates** (per Raspberry Pi)
   - Subject: CN=client-{mac-address}
   - Extended Key Usage: Client Authentication
   - Validity: 2 years
   - Embedded during image generation

5. **iOS Certificates**
   - Subject: CN=ios-{device-id}
   - Installed via MDM or manual import
   - Validity: 1 year

### Mutual TLS Authentication

```
Client                          Server
  │                               │
  │ ─────── ClientHello ────────▶ │
  │                               │
  │ ◀────── ServerHello ───────── │
  │         ServerCertificate     │
  │         CertificateRequest    │
  │                               │
  │ ─────── ClientCertificate ──▶ │
  │         ClientKeyExchange     │
  │         CertificateVerify     │
  │                               │
  │ ◀─────── Finished ──────────  │
  │                               │
  │ ───── Application Data ─────▶ │
  │ ◀──── Application Data ─────  │
```

**Verification Steps:**
1. Server presents certificate, client validates against CA
2. Server requests client certificate
3. Client presents certificate, server validates
4. Both parties verify certificate chains
5. Establish encrypted channel with TLS 1.3
6. Begin application data exchange

### Role-Based Access Control (RBAC)

**Roles:**

1. **Administrator**
   - Full system access
   - User management
   - Certificate management
   - System configuration

2. **Content Manager**
   - Upload/edit content
   - Manage overlays
   - Schedule playlists
   - View analytics

3. **Operator**
   - View dashboards
   - Restart clients
   - View logs
   - Basic troubleshooting

4. **Viewer**
   - Read-only dashboard access
   - No configuration changes

**AD/LDAP Integration:**
- Automatic group synchronization
- SSO support (Windows Integrated Auth)
- Password policy enforcement
- Account lockout after failed attempts

### Audit Logging

**Logged Events:**
- User authentication (success/failure)
- Content uploads/modifications
- Client registration/deregistration
- Configuration changes
- Certificate operations
- Privilege escalations
- Export operations
- Failed access attempts

**Log Storage:**
- SQL Server table (encrypted at rest)
- Tamper-proof (append-only)
- Retention: 1 year minimum
- Export to SIEM systems (Syslog, CEF)

**Log Format:**
```json
{
  "timestamp": "2024-01-15T10:30:45.123Z",
  "event_id": "AUTH_SUCCESS",
  "user": "admin@company.local",
  "ip_address": "10.0.1.25",
  "action": "User logged in",
  "resource": "WPF Management App",
  "result": "SUCCESS",
  "details": {
    "auth_method": "LDAP",
    "session_id": "abc123..."
  }
}
```

## Network Architecture

### Auto-Discovery (mDNS)

**Service Advertisement:**

Server advertises:
```
_makerscreen-server._tcp.local. → port 8443
_makerscreen-api._tcp.local. → port 8080
```

Clients advertise:
```
_makerscreen-client._tcp.local. → port 8080
makerscreen-{mac}._http._tcp.local. → web UI
```

**Discovery Flow:**
1. Client broadcasts mDNS query for `_makerscreen-server._tcp.local`
2. Server responds with IP address and port
3. Client initiates WSS connection with certificate
4. Mutual TLS handshake
5. Client registers with server (sends hardware info)
6. Server assigns client ID and downloads configuration
7. Client starts normal operation

### Network Topology

**Recommended Setup:**

```
┌────────────────────────────────────────┐
│  Management VLAN (10.0.1.0/24)         │
│  ┌──────────────┐  ┌─────────────┐    │
│  │  Server      │  │  Admin PCs  │    │
│  │  10.0.1.10   │  │             │    │
│  └──────────────┘  └─────────────┘    │
└────────────────────────────────────────┘
                 │
          ┌──────┴──────┐
          │   Router    │
          └──────┬──────┘
                 │
┌────────────────────────────────────────┐
│  Display VLAN (10.0.2.0/24)            │
│  ┌────────┐ ┌────────┐  ┌────────┐    │
│  │Client 1│ │Client 2│  │Client N│    │
│  │.2.11   │ │.2.12   │  │.2.N    │    │
│  └────────┘ └────────┘  └────────┘    │
└────────────────────────────────────────┘
```

**Firewall Rules:**

Management VLAN → Server:
- Allow TCP 8443 (WSS)
- Allow TCP 8080 (REST API)
- Allow TCP 1433 (SQL Server - admin only)

Display VLAN → Server:
- Allow TCP 8443 (WSS)
- Allow UDP 5353 (mDNS)

iOS Devices → Server:
- Allow TCP 8443 (WSS via WiFi or VPN)
- Allow TCP 8080 (REST API)

All → Internet:
- DENY (explicit block)

## Data Flow

### Content Distribution

```
┌──────────────────────────────────────────────┐
│  1. Content Upload                           │
│     ┌────────┐                               │
│     │ Admin  │ uploads PNG                   │
│     └───┬────┘                               │
│         │                                    │
│         ▼                                    │
│  ┌──────────────┐                           │
│  │  WPF App     │                           │
│  │  - Validate  │                           │
│  │  - Optimize  │                           │
│  │  - Generate  │                           │
│  │    Thumbnail │                           │
│  └──────┬───────┘                           │
└─────────┼────────────────────────────────────┘
          │
          ▼
┌──────────────────────────────────────────────┐
│  2. Database Storage                         │
│  ┌──────────────────────────────────────┐   │
│  │  SQL Server                          │   │
│  │  - Content FILESTREAM                │   │
│  │  - Metadata table                    │   │
│  │  - Version history                   │   │
│  └──────────────────────────────────────┘   │
└─────────┼────────────────────────────────────┘
          │
          ▼
┌──────────────────────────────────────────────┐
│  3. Client Notification (SignalR)            │
│     "New content available: content-123"     │
└─────────┼────────────────────────────────────┘
          │
          ▼
┌──────────────────────────────────────────────┐
│  4. Client Download                          │
│  ┌──────────────┐                           │
│  │  Client      │ WSS Request                │
│  │  - Check     │ "GET /content/123"         │
│  │    cache     │                            │
│  │  - Download  │ ◀── Delta or Full          │
│  │    if new    │                            │
│  └──────┬───────┘                           │
└─────────┼────────────────────────────────────┘
          │
          ▼
┌──────────────────────────────────────────────┐
│  5. Display Content                          │
│  ┌──────────────────────────────────────┐   │
│  │  PyQt5 Display Engine                │   │
│  │  - Load content                      │   │
│  │  - Apply overlays                    │   │
│  │  - Render with GPU                   │   │
│  └──────────────────────────────────────┘   │
└──────────────────────────────────────────────┘
```

### Real-Time Overlay Data

```
SQL Database ──┐
               │
API Endpoint ──┼──▶ Server Overlay Engine
               │      │
File System ───┘      │ Query every N seconds
                      │ (configurable interval)
                      ▼
               ┌──────────────┐
               │  Process     │
               │  - Transform │
               │  - Format    │
               │  - Cache     │
               └──────┬───────┘
                      │
                      ▼ WSS Push
               ┌──────────────┐
               │  Client      │
               │  - Update    │
               │    overlay   │
               │  - Re-render │
               └──────────────┘
```

## Component Details

### Server: Image Builder

**Functionality:**
Creates bootable Raspberry Pi OS images with all components pre-installed.

**Process:**
1. Download base Raspberry Pi OS Lite image
2. Mount image (loop device)
3. Chroot into image
4. Install system updates
5. Install Python 3.11 and dependencies
6. Copy MakerScreen client application
7. Configure systemd services
8. Set up network configuration
9. Embed SSL certificates
10. Configure auto-start
11. Unmount and compress image

**Output:**
- `.img` file ready for SD card writing
- `.img.gz` compressed for download
- Checksum file (SHA256)

**Customization:**
- WiFi SSID/password pre-configured
- Static IP or DHCP
- Hostname pattern
- Timezone settings
- Server connection details

### Client: Status Screen Generator

**Screen Layouts:**

**Layout 1: System Information**
```
┌────────────────────────────────────────┐
│  MakerScreen Client Status             │
│                                        │
│  Hostname: display-lobby-01            │
│  IP Address: 10.0.2.15                 │
│  MAC: b8:27:eb:12:34:56                │
│  Version: 1.2.3                        │
│                                        │
│  Status: ⚠ Searching for server...     │
│  Attempt: 42 (Next in 30s)             │
│                                        │
│  CPU: 45% | RAM: 320MB / 1GB           │
│  Temp: 54°C | GPU: Active              │
└────────────────────────────────────────┘
```

**Layout 2: QR Code Access**
```
┌────────────────────────────────────────┐
│  Need to configure this display?       │
│                                        │
│     Scan QR code with your phone:      │
│                                        │
│        ┌───────────────────┐           │
│        │ █▀▀▀▀▀█ ▄▄ █▀▀▀▀▀█│           │
│        │ █ ███ █ ██ █ ███ █│           │
│        │ █ ▀▀▀ █ ▀  █ ▀▀▀ █│           │
│        │ ▀▀▀▀▀▀▀ ▀ ▀ ▀▀▀▀▀▀│           │
│        │ ▄█ ▀█▄▀ ██▀▀██▄▄▀ │           │
│        │ █▀▀▀▀▀█ ▀█ ▀█ ▀ ▄█│           │
│        └───────────────────┘           │
│                                        │
│  Or visit: http://10.0.2.15:8080      │
│                                        │
│  Status: 🔴 Not Connected              │
└────────────────────────────────────────┘
```

**Layout 3: Network Diagnostics**
```
┌────────────────────────────────────────┐
│  Network Diagnostics                   │
│                                        │
│  ✓ Network Interface: eth0 UP          │
│  ✓ IP Address: 10.0.2.15 (DHCP)        │
│  ✓ Gateway: 10.0.2.1 (reachable)       │
│  ✓ DNS: 8.8.8.8 (responding)           │
│                                        │
│  ✗ Server Discovery: Not found         │
│    Looking for _makerscreen-server     │
│                                        │
│  Troubleshooting Tips:                 │
│  1. Ensure server is running           │
│  2. Check firewall allows port 8443    │
│  3. Verify same network segment        │
│                                        │
│  Run diagnostics via web UI ───────▶   │
└────────────────────────────────────────┘
```

**Rotation:**
- Each screen displays for 15 seconds
- Smooth fade transitions
- Auto-refresh data every 30 seconds
- QR code regenerated with current IP

### iOS: Push Notification System

**Notification Types:**

1. **Client Offline**
   - Title: "Display Offline"
   - Body: "Lobby Display 1 has lost connection"
   - Actions: [View Details, Dismiss]
   - Priority: High

2. **Content Update Complete**
   - Title: "Content Updated"
   - Body: "New content deployed to 12 displays"
   - Actions: [View Report, OK]
   - Priority: Normal

3. **Error Alert**
   - Title: "System Error"
   - Body: "3 displays failed to update content"
   - Actions: [View Logs, Retry, Dismiss]
   - Priority: High

4. **Scheduled Task**
   - Title: "Playlist Change"
   - Body: "Evening playlist will start in 5 minutes"
   - Actions: [View Schedule, OK]
   - Priority: Low

**Implementation:**
- APNs with token-based authentication
- Server maintains device token registry
- Background notification processing
- Action handling with deep links
- Notification grouping by type

## Integration Points

### SQL Server Data Sources

**Configuration:**
```json
{
  "dataSources": [
    {
      "id": "sales-db",
      "type": "sql-server",
      "connectionString": "Server=sql.company.local;Database=Sales;Integrated Security=true",
      "queries": {
        "daily-revenue": {
          "sql": "EXEC sp_GetDailyRevenue @Date",
          "parameters": {
            "@Date": "TODAY"
          },
          "refreshInterval": 300
        }
      }
    }
  ]
}
```

**Supported Operations:**
- Stored procedures
- Parameterized queries
- Views
- Multiple result sets
- Transactions (read-only)

### REST API

**Endpoints:**

```
Authentication:
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout

Content:
GET    /api/v1/content
POST   /api/v1/content
GET    /api/v1/content/{id}
PUT    /api/v1/content/{id}
DELETE /api/v1/content/{id}

Clients:
GET    /api/v1/clients
GET    /api/v1/clients/{id}
PUT    /api/v1/clients/{id}
POST   /api/v1/clients/{id}/reboot
GET    /api/v1/clients/{id}/screenshot
GET    /api/v1/clients/{id}/logs

Playlists:
GET    /api/v1/playlists
POST   /api/v1/playlists
GET    /api/v1/playlists/{id}
PUT    /api/v1/playlists/{id}
DELETE /api/v1/playlists/{id}

Overlays:
GET    /api/v1/overlays
POST   /api/v1/overlays
GET    /api/v1/overlays/{id}
PUT    /api/v1/overlays/{id}
DELETE /api/v1/overlays/{id}
```

**Authentication:**
- Bearer token (JWT)
- Mutual TLS required
- Token expiry: 1 hour
- Refresh token: 30 days

### Active Directory / LDAP

**Configuration:**
```json
{
  "ldap": {
    "server": "ldap://ad.company.local:389",
    "baseDN": "DC=company,DC=local",
    "bindDN": "CN=MakerScreen Service,OU=Services,DC=company,DC=local",
    "groupMapping": {
      "CN=MakerScreen Admins,OU=Groups,DC=company,DC=local": "Administrator",
      "CN=MakerScreen Content,OU=Groups,DC=company,DC=local": "Content Manager",
      "CN=MakerScreen Operators,OU=Groups,DC=company,DC=local": "Operator",
      "CN=MakerScreen Viewers,OU=Groups,DC=company,DC=local": "Viewer"
    },
    "syncInterval": 3600
  }
}
```

**Features:**
- Automatic user provisioning
- Group synchronization
- Password validation against AD
- Account lockout policy enforcement
- SSO with Windows Integrated Authentication

## Performance & Scalability

### Server Capacity

**Hardware Requirements (100 Clients):**
- CPU: 4 cores, 2.5 GHz+
- RAM: 16 GB
- Storage: 500 GB SSD (OS + Content)
- Network: 1 Gbps NIC

**Performance Metrics:**
- WebSocket connections: 500 concurrent
- Content delivery: 100 Mbps sustained
- Database queries: < 100ms p95
- API response time: < 200ms p95

### Client Performance

**Raspberry Pi 4 (4GB):**
- Boot to content: < 60 seconds
- Content switching: < 1 second
- Overlay refresh: 60 FPS
- Memory usage: < 500 MB
- CPU usage: < 30% average

### Network Optimization

**Bandwidth Management:**
- Delta updates reduce bandwidth by 80%
- Content compression (WebP)
- QoS prioritization
- Scheduled updates during off-hours
- P2P content distribution (optional)

**Traffic Estimates (per client/day):**
- Heartbeat: ~5 KB
- Overlay data: ~500 KB
- Content updates: ~50 MB (average)
- Logs: ~10 KB
- **Total: ~50 MB/day per client**

**100 Clients:**
- Total: ~5 GB/day
- Peak: ~100 Mbps (simultaneous updates)

## Disaster Recovery

### Backup Strategy

**Server Backups:**
- Database: Full backup daily, transaction log hourly
- Content repository: Incremental daily
- Configuration: Daily export to JSON
- Certificates: Encrypted backup to secure location

**Client Resilience:**
- Local content cache survives reboot
- Offline mode with cached playlists
- Automatic server reconnection
- Fallback to last known good content

### Recovery Procedures

**Server Failure:**
1. Deploy standby server (< 30 minutes)
2. Restore database from backup
3. Restore content repository
4. Update DNS/mDNS for new server IP
5. Clients auto-discover and reconnect

**Client Failure:**
1. Replace SD card with pre-imaged backup
2. Boot client
3. Auto-discovers server
4. Downloads latest content
5. Resumes operation (< 5 minutes)

## Monitoring & Alerting

### Health Metrics

**Server:**
- CPU, RAM, Disk usage
- WebSocket connection count
- Database performance
- Content delivery throughput
- Error rates

**Clients:**
- Online/offline status
- Heartbeat interval
- Display status
- Temperature
- Content version
- Network quality

### Alert Conditions

1. **Critical:**
   - Server down
   - Database unavailable
   - >50% clients offline
   - Certificate expiring < 7 days

2. **Warning:**
   - High CPU/RAM usage (>80%)
   - Client offline > 5 minutes
   - Failed content delivery
   - Disk space < 20%

3. **Info:**
   - New client registered
   - Content uploaded
   - Scheduled task completed

### Dashboards

**Real-Time Dashboard (SignalR):**
- Live client status map
- Connection health
- Active content distribution
- System metrics (CPU, RAM, Network)
- Recent alerts

**Analytics Dashboard:**
- Content performance (view duration, frequency)
- Client uptime statistics
- Bandwidth usage trends
- Error analysis
- User activity logs

---

## Conclusion

The MakerScreen architecture provides a robust, secure, and scalable Digital Signage Management System with zero-touch deployment capabilities. The combination of Windows server, Raspberry Pi clients, and iOS management creates a cohesive ecosystem that prioritizes security, ease of use, and reliability.

The system is designed for complete network isolation while maintaining full functionality, with comprehensive auto-discovery and self-healing mechanisms ensuring minimal administrative overhead even at scale.
