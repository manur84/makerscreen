# MakerScreen Build & Deployment Guide

## Prerequisites

### Server (Windows)
- .NET 8 SDK
- SQL Server 2019+ or SQL Server Express
- Visual Studio 2022 (optional)

### Client (Raspberry Pi)
- Raspberry Pi OS (64-bit recommended)
- Python 3.11+
- PyQt5, Flask, websockets

### iOS
- Xcode 15+
- macOS Ventura or later
- iOS 17+ SDK

## Building

### Server

```powershell
cd Server
dotnet restore
dotnet build --configuration Release
```

### Client

```bash
cd Client
pip3 install -r requirements.txt
```

### iOS

```bash
cd iOS/MakerScreen
# Open MakerScreen.xcodeproj in Xcode and build
```

## Running

### Server

```powershell
cd Server/MakerScreen.Server
dotnet run
```

Or open `Server/MakerScreen.sln` in Visual Studio and press F5.

### Client

```bash
cd Client
python3 main.py
```

For production, install as systemd service:

```bash
sudo cp makerscreen.service /etc/systemd/system/
sudo cp makerscreen-webui.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable makerscreen
sudo systemctl enable makerscreen-webui
sudo systemctl start makerscreen
sudo systemctl start makerscreen-webui
```

### iOS

Build and run in Xcode simulator or deploy to physical device.

## Testing

### Server

```powershell
cd Server
dotnet test
```

### Client

```bash
cd Client
pytest
```

## Project Structure

```
/MakerScreen
├── Server/                      # Windows .NET Server
│   ├── MakerScreen.sln         # Solution file
│   ├── MakerScreen.Server/     # WPF Application
│   │   ├── Views/              # XAML views
│   │   ├── ViewModels/         # MVVM view models
│   │   └── Services/           # Application services
│   ├── MakerScreen.Core/       # Domain models
│   │   └── Models/             # Entity models
│   ├── MakerScreen.Data/       # EF Core
│   │   └── Context/            # DbContext
│   └── MakerScreen.WebSocket/  # WebSocket server
│       └── Services/           # WebSocket services
│
├── Client/                      # Raspberry Pi Client
│   ├── main.py                 # Main client application
│   ├── webui.py                # Flask web UI
│   ├── requirements.txt        # Python dependencies
│   ├── templates/              # HTML templates
│   ├── static/                 # Static assets
│   ├── makerscreen.service     # systemd service file
│   └── makerscreen-webui.service
│
├── iOS/                         # iOS Management App
│   └── MakerScreen/
│       ├── MakerScreenApp.swift
│       ├── Views/              # SwiftUI views
│       ├── ViewModels/         # View models
│       ├── Models/             # Data models
│       └── Services/           # WebSocket, API services
│
├── docs/                        # Documentation
│   └── DATABASE_SCHEMA.md      # Database schema
│
├── PLAN.md                      # Implementation plan
└── README.md                    # This file
```

## Configuration

### Server

Connection string in `Server/MakerScreen.Server/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MakerScreen;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "WebSocket": {
    "Port": 8080,
    "EnableTLS": true
  }
}
```

### Client

Create `/opt/makerscreen/config.json`:

```json
{
  "server_url": "ws://server-ip:8080/ws/",
  "web_ui_port": 8080,
  "display": {
    "fullscreen": true,
    "rotation": 0
  }
}
```

### iOS

Configure server URL in Settings tab.

## First-Time Setup

1. **Build and run server**
   ```powershell
   cd Server/MakerScreen.Server
   dotnet run
   ```

2. **Start WebSocket server** from UI (Settings tab → Start WebSocket Server)

3. **Run client** on Raspberry Pi
   ```bash
   cd Client
   python3 main.py
   ```

4. **Client connects automatically** via mDNS discovery

5. **Configure via web UI** - scan QR code on client display or navigate to `http://client-ip:8080`

## Troubleshooting

### Server won't start
- Check SQL Server is running
- Verify connection string in appsettings.json
- Check firewall allows port 8080

### Client can't connect
- Verify server WebSocket is running
- Check network connectivity
- Review client logs: `/var/log/makerscreen/client.log`
- Access web UI at `http://client-ip:8080` for diagnostics

### iOS app won't connect
- Verify server URL in Settings
- Check network allows WebSocket connections
- Review Xcode console for errors
