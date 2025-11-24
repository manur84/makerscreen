# Implementation Plan

## Phase 1: Server Core (.NET 8 WPF)

### 1.1 Project Setup
```
Solution: MakerScreen.sln
├── MakerScreen.Server (WPF)
├── MakerScreen.Core (Shared)
├── MakerScreen.Data (EF Core)
└── MakerScreen.WebSocket (WSS Server)
```

### 1.2 Database (EF Core + SQL Server)
- Users, Roles, Permissions (RBAC)
- Content (FILESTREAM for PNGs)
- Clients (registry, status, metrics)
- Overlays, DataSources, Queries
- Playlists, Schedules
- AuditLog (append-only)

See: DATABASE_SCHEMA.md

### 1.3 WebSocket Server
```csharp
// TLS 1.3, Mutual Auth
- Client registration
- Heartbeat protocol
- Content push
- Command execution
```

### 1.4 WPF UI (MVVM)
- Login (AD/LDAP)
- Dashboard (client status)
- Content manager (drag/drop PNG)
- Overlay designer (visual)
- Client manager
- Scheduling

### 1.5 Services
- Certificate generator (Root CA → certs)
- mDNS discovery
- Image builder (Raspberry Pi OS + Python client)

## Phase 2: Client (Python/Raspberry Pi)

### 2.1 Project Structure
```
/client
├── main.py (systemd service)
├── display.py (PyQt5 rendering)
├── webui.py (Flask)
├── status_screen.py (QR code + diagnostics)
└── network.py (mDNS, WebSocket)
```

### 2.2 Core Functions
- WebSocket client (WSS, auto-reconnect)
- Content download/cache
- Display renderer (GPU-accelerated)
- Overlay engine (SQL data → rendering)
- Status screens (3 layouts, rotating)

### 2.3 Local Web UI (Flask)
```
Routes:
/               Dashboard
/network        WiFi/Ethernet config
/display        Resolution, rotation
/diagnostics    Ping, logs
/emergency      Manual content upload
```

### 2.4 Auto-Setup
- mDNS server discovery
- Certificate validation
- Self-registration
- systemd service

## Phase 3: iOS App (SwiftUI)

### 3.1 Project Structure
```
/iOS/MakerScreen
├── Views/
├── ViewModels/
├── Services/
└── Models/
```

### 3.2 Features
- WebSocket connection
- Client list/status
- Content upload (Photos)
- Remote screenshot
- Push notifications
- QR scanner (→ client web UI)

## Phase 4: Image Builder

### 4.1 Automated Image Creation
```python
# Download Raspberry Pi OS
# Mount, chroot
# Install: Python 3.11, dependencies
# Copy client code
# Embed certificates
# Configure systemd
# Unmount → .img file
```

## Implementation Order

**Week 1-2**: Database schema + EF Core models  
**Week 3-4**: WebSocket server + certificate management  
**Week 5-6**: WPF UI basics (login, dashboard, content)  
**Week 7-8**: Overlay designer + SQL integration  
**Week 9-10**: Python client (display + WebSocket)  
**Week 11-12**: Status screens + local web UI  
**Week 13-14**: iOS app core  
**Week 15-16**: Image builder + testing

## Critical Components

1. **Certificate System**
   - Root CA generation
   - Cert signing (server, clients)
   - Auto-renewal
   - CRL management

2. **WebSocket Protocol**
   ```json
   {
     "type": "register|heartbeat|content|command",
     "data": { ... }
   }
   ```

3. **Status Screens** (when offline)
   - Screen 1: System info
   - Screen 2: QR code (→ http://IP:8080)
   - Screen 3: Network diagnostics
   - Rotate every 15s

4. **Overlay Rendering**
   ```python
   # Load PNG base
   # Query SQL → data
   # Render text/charts/QR on layers
   # Composite → display
   ```

## Testing Strategy

- Unit tests (C#, Python)
- Integration tests (WebSocket protocol)
- End-to-end (Server → Client → Display)
- Security tests (cert validation, TLS)
- Load tests (100+ clients)

## Deployment

**Server**: Single MSI installer  
**Client**: Pre-built .img for SD card  
**iOS**: TestFlight → App Store
