# MakerScreen - Technical Specification

## System Architecture

```
Server (Windows/.NET 8)  ←WSS/TLS1.3→  Client (Raspberry Pi/Python)
         ↓                                      ↓
    SQL Server                          Local Web UI (Flask)
         ↓
    iOS App (SwiftUI)
```

## Technology Stack

**Server**: .NET 8, WPF, EF Core, SignalR, WebSockets  
**Client**: Python 3.11, PyQt5, Flask, Zeroconf  
**iOS**: Swift 5.9+, SwiftUI, Combine  
**Database**: SQL Server 2019+

## Security

- TLS 1.3 mutual auth
- Self-signed CA (Root → Intermediate → Certs)
- Network isolation (no Internet)
- RBAC with AD/LDAP
- Audit logging

## Core Features

1. **Zero-Touch Deployment**: Auto image builder, mDNS discovery
2. **Smart Status Screens**: Display diagnostics when offline
3. **Dynamic Overlays**: SQL data on PNG layouts
4. **Local Web UI**: Client configuration via QR code
5. **Self-Healing**: Auto-recovery, offline mode

## Implementation Plan

See [DATABASE_SCHEMA.md](docs/DATABASE_SCHEMA.md) for complete database design.
