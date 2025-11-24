# MakerScreen - Digital Signage Management System

## 🎯 Vision
A highly secure, enterprise-grade Digital Signage Management System with zero-touch deployment, designed for complete network isolation and ease of use.

## 🏗️ System Architecture

### Core Components

1. **Server Component** (Windows/.NET 8 WPF)
   - Zero-touch client deployment with integrated image builder
   - Dynamic overlay system with SQL data integration
   - Comprehensive client management dashboard
   - Scheduling and playlist management
   - Secure WebSocket server (WSS with TLS 1.3)

2. **Client Component** (Raspberry Pi/Python)
   - Vollautomatisches setup with auto-discovery
   - Smart status screens with QR code access
   - Local web UI for configuration
   - Hardware-accelerated display management
   - Self-healing mechanisms

3. **iOS Management App** (SwiftUI)
   - Mobile-first management interface
   - Real-time monitoring with push notifications
   - AR-based display identification
   - Quick actions and widgets

4. **Database** (SQL Server)
   - Content repository with FILESTREAM
   - Complete audit trail
   - Performance metrics and analytics

## 🔒 Security Features

- **Network Isolation**: Local network only, no Internet exposure
- **WSS-Only Communication**: TLS 1.3 encrypted WebSocket connections
- **Certificate Management**: Self-signed CA with Mutual TLS
- **RBAC**: Role-based access control with AD/LDAP integration
- **Audit Logging**: Complete system access and change tracking

## ⚡ Key Features

### Zero-Touch Deployment
- Server installation: < 5 minutes
- Client deployment: < 3 minutes per Raspberry Pi
- iOS app setup: < 1 minute
- First content display: < 10 minutes total

### Smart Status Screens
When clients lack server connection or content:
- Rotating system information displays
- Large QR code linking to local web UI
- Color-coded status indicators
- Network diagnostics and troubleshooting

### Dynamic Content Overlays
- Visual designer with multi-layer support
- Real-time SQL data integration
- Text, charts, QR codes, date/time
- Template system for reusable designs

## 📊 Project Structure

```
/MakerScreen
├── /docs                    # Documentation
│   ├── ARCHITECTURE.md     # Detailed architecture
│   ├── IMPLEMENTATION_ROADMAP.md
│   ├── SECURITY.md
│   ├── DATABASE_SCHEMA.md
│   └── DEPLOYMENT.md
├── /Server                 # Windows .NET 8 Server
│   ├── /WPF.Management
│   ├── /WebSocketServer
│   ├── /Services
│   └── /Database
├── /Client                 # Raspberry Pi Client
│   ├── /RaspberryPi
│   ├── /WebUI
│   └── /SystemConfig
├── /iOS                    # iOS Management App
│   ├── /MakerScreenApp
│   └── /Shared
└── /Deployment
    ├── /ImageBuilder
    ├── /Installers
    └── /Scripts
```

## 🚀 Quick Start

### Server Installation
```powershell
.\InstallMakerScreenServer.exe
# Complete setup in < 5 minutes
```

### Client Deployment
1. Download Raspberry Pi image from server UI
2. Flash to SD card
3. Boot Raspberry Pi
4. Client auto-discovers and registers

### iOS App
1. Download from TestFlight/App Store
2. Scan QR code from server
3. Start managing displays

## 📈 Scalability

- Designed for 100+ displays
- Load balancing for large installations
- Bandwidth optimization with delta updates
- Intelligent content caching

## 🛠️ Technology Stack

- **Server**: C#/.NET 8, WPF, Entity Framework Core, SignalR
- **Client**: Python 3.11+, PyQt5, Flask, asyncio
- **iOS**: Swift 5.9+, SwiftUI, Combine
- **Database**: SQL Server 2019+
- **Protocols**: WebSocket Secure (WSS), REST API, mDNS

## 📝 License

[To be determined]

## 🤝 Contributing

[Contributing guidelines to be added]

## 📧 Contact

[Contact information to be added]
