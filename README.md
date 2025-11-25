# MakerScreen Digital Signage System

🎯 **Enterprise-grade Digital Signage Management System with Zero-Touch Client Deployment**

MakerScreen is a complete solution for managing digital signage displays across your organization. The system features automatic client deployment, eliminating all manual setup required for Raspberry Pi display devices.

## ✨ Key Features

### 🚀 Zero-Touch Deployment
- **Automatic Client Installation**: Deploy to Raspberry Pi devices without any manual intervention
- **Network Auto-Discovery**: Automatically finds and configures Raspberry Pi devices on your network
- **Pre-configured Images**: Generate bootable SD card images with client pre-installed
- **One-Click Deployment**: Deploy to multiple devices simultaneously from the management console

### 🖥️ Server Application (Windows/.NET 8)
- **WPF Management Console**: Intuitive graphical interface for managing your signage network
- **WebSocket Server**: Secure, real-time communication with all connected clients
- **Content Management**: Upload and distribute images, videos, and HTML content
- **Client Monitoring**: Real-time status monitoring of all connected displays
- **Deployment Engine**: Built-in tools for automatic client deployment

### 📺 Client Application (Raspberry Pi/Python)
- **Automatic Connection**: Connects to server automatically on boot
- **Content Display**: Displays images, videos, and web content
- **Self-Healing**: Automatically reconnects if connection is lost
- **Low Resource Usage**: Optimized for Raspberry Pi hardware
- **Remote Management**: Fully controllable from the server

### 🔒 Security
- **WebSocket Secure (WSS)**: Encrypted communication (production-ready)
- **Authentication**: Client authentication and authorization
- **Network Isolation**: Supports isolated network deployments
- **Audit Logging**: Complete logging of all system activities

## 📋 System Requirements

### Server Requirements
- **OS**: Windows 10/11 or Windows Server 2019+
- **.NET**: .NET 8 Runtime
- **RAM**: 4GB minimum, 8GB recommended
- **Network**: Static IP address recommended
- **Ports**: TCP 8443 (WebSocket server)

### Client Requirements
- **Hardware**: Raspberry Pi 3B+ or newer
- **OS**: Raspberry Pi OS (Bullseye or newer)
- **Network**: Ethernet or Wi-Fi connection to server
- **Display**: HDMI-compatible display

## 🚀 Quick Start

### Option 1: Using the WPF Management Console (Recommended)

1. **Start the Management Console**:
   ```bash
   cd Server/MakerScreen.Management
   dotnet run
   ```

2. **Deploy Clients**:
   - Click "🚀 Auto-Deploy Clients" to automatically discover and deploy to Raspberry Pi devices
   - Or click "📦 Create Deployment Package" to create a manual installation package
   - Or click "💿 Generate Raspberry Pi Image" to create a bootable SD card image

3. **Add Content**:
   - Click "➕ Add Content" to upload images or videos
   - Select content and click "Push" to send it to all connected displays

### Option 2: Using Pre-built SD Card Image

1. **Generate the Image** (on server):
   ```bash
   cd Deployment/ImageBuilder
   chmod +x build-image.sh
   ./build-image.sh
   ```

2. **Flash to SD Card**:
   ```bash
   # Using Balena Etcher (recommended)
   # Or using dd:
   sudo dd if=makerscreen-raspios.img.xz of=/dev/sdX bs=4M status=progress
   ```

3. **Configure Server Address**:
   - Edit `config.json` on the boot partition
   - Set `serverUrl` to your server's IP address

4. **Boot Raspberry Pi**:
   - Insert SD card and power on
   - Client will auto-start and connect to server

### Option 3: Manual Client Installation

1. **Copy Files to Raspberry Pi**:
   ```bash
   scp Client/RaspberryPi/* pi@raspberrypi:/tmp/
   ```

2. **Run Installation Script**:
   ```bash
   ssh pi@raspberrypi
   cd /tmp
   chmod +x install.sh
   ./install.sh
   ```

3. **Edit Configuration**:
   ```bash
   sudo nano /opt/makerscreen/config.json
   # Set serverUrl to your server's IP
   ```

4. **Start Service**:
   ```bash
   sudo systemctl start makerscreen
   sudo systemctl status makerscreen
   ```

## 📁 Project Structure

```
MakerScreen/
├── Server/
│   ├── MakerScreen.Core/           # Shared models and interfaces
│   ├── MakerScreen.Services/       # Business logic services
│   │   ├── WebSocket/              # WebSocket server implementation
│   │   ├── Deployment/             # Client deployment engine
│   │   └── Content/                # Content management
│   ├── MakerScreen.Server/         # Console server application
│   └── MakerScreen.Management/     # WPF management console
│       ├── ViewModels/             # MVVM ViewModels
│       └── Views/                  # WPF views
├── Client/
│   ├── RaspberryPi/                # Python client for Raspberry Pi
│   │   ├── client.py               # Main client application
│   │   ├── requirements.txt        # Python dependencies
│   │   ├── install.sh              # Installation script
│   │   └── makerscreen.service     # Systemd service file
│   └── iOS/                        # iOS management app (SwiftUI)
│       └── MakerScreen/            # Swift Package
│           ├── Sources/            # Swift source code
│           └── Tests/              # Unit tests
├── Deployment/
│   ├── ImageBuilder/               # Raspberry Pi image builder
│   └── Scripts/                    # Deployment automation scripts
└── Documentation/
```

## 🔧 Configuration

### Server Configuration

The server is configured through the `appsettings.json` file:

```json
{
  "Server": {
    "Port": 8443,
    "EnableSSL": true,
    "CertificatePath": "./certs/server.pfx"
  },
  "Deployment": {
    "AutoDiscovery": true,
    "NetworkRange": "192.168.1.0/24"
  }
}
```

### Client Configuration

Client configuration is stored in `/opt/makerscreen/config.json`:

```json
{
  "serverUrl": "ws://192.168.1.100:8443",
  "autoStart": true,
  "reconnectInterval": 5,
  "heartbeatInterval": 30
}
```

## 🎯 Usage Guide

### Deploying Multiple Clients

1. **Prepare Raspberry Pi Devices**:
   - Flash Raspberry Pi OS to SD cards
   - Enable SSH (create empty `ssh` file on boot partition)
   - Boot devices on same network as server

2. **Run Auto-Deploy**:
   - Open Management Console
   - Click "🚀 Auto-Deploy Clients"
   - System will discover and deploy to all Raspberry Pi devices

3. **Verify Deployment**:
   - Check "Connected Clients" panel
   - All deployed clients should appear within 30 seconds

### Managing Content

1. **Add Content**:
   - Click "➕ Add Content"
   - Select image/video file
   - Content is automatically uploaded to server

2. **Distribute Content**:
   - Select content from library
   - Click "Push" to send to all displays
   - Content appears on displays within seconds

3. **Schedule Content**:
   - Content scheduling (future enhancement)
   - Playlist management (future enhancement)

### Monitoring Clients

The Management Console provides real-time monitoring:
- **Status**: Online/Offline/Error status
- **IP Address**: Network location of each client
- **Version**: Client software version
- **Last Seen**: Last heartbeat timestamp

## 🔐 Security Best Practices

1. **Change Default Passwords**:
   - Change default Raspberry Pi password: `passwd`
   - Use strong passwords for all accounts

2. **Enable SSL/TLS**:
   - Generate SSL certificates for production
   - Configure WSS (WebSocket Secure) instead of WS

3. **Firewall Configuration**:
   - Restrict access to port 8443 to local network only
   - Use network segmentation for signage network

4. **Regular Updates**:
   - Keep server and client software updated
   - Update Raspberry Pi OS regularly

## 🛠️ Development

### Building from Source

**Prerequisites**:
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Python 3.9+ (for client development)

**Build Server**:
```bash
cd Server
dotnet build MakerScreen.sln
```

**Run Management Console**:
```bash
cd Server/MakerScreen.Management
dotnet run
```

**Run Console Server**:
```bash
cd Server/MakerScreen.Server
dotnet run
```

**Test Client Locally**:
```bash
cd Client/RaspberryPi
pip3 install -r requirements.txt
python3 client.py
```

## 📊 Architecture

### System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Management Console (WPF)                  │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Deployment  │  │   Content    │  │  Monitoring  │      │
│  │    Engine    │  │  Management  │  │   Dashboard  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ WebSocket (WSS)
                              │
┌─────────────────────────────────────────────────────────────┐
│                    WebSocket Server (.NET)                   │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Connection  │  │   Message    │  │    Client    │      │
│  │   Manager    │  │   Handler    │  │   Registry   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ WebSocket (WSS)
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
┌───────────────┐     ┌───────────────┐     ┌───────────────┐
│ Raspberry Pi  │     │ Raspberry Pi  │     │ Raspberry Pi  │
│   Client 1    │     │   Client 2    │     │   Client 3    │
│               │     │               │     │               │
│  ┌─────────┐  │     │  ┌─────────┐  │     │  ┌─────────┐  │
│  │ Display │  │     │  │ Display │  │     │  │ Display │  │
│  │ Engine  │  │     │  │ Engine  │  │     │  │ Engine  │  │
│  └─────────┘  │     │  └─────────┘  │     │  └─────────┘  │
└───────────────┘     └───────────────┘     └───────────────┘
```

### Communication Protocol

All communication uses WebSocket with JSON messages:

```json
{
  "type": "CONTENT_UPDATE",
  "clientId": "b827eb123456",
  "data": {
    "contentId": "uuid",
    "name": "welcome.png",
    "type": "Image",
    "data": "base64encoded..."
  },
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## 🐛 Troubleshooting

### Client Not Connecting

1. **Check Network**:
   ```bash
   ping SERVER_IP
   ```

2. **Verify Server Running**:
   ```bash
   # On server
   netstat -an | grep 8443
   ```

3. **Check Client Logs**:
   ```bash
   sudo journalctl -u makerscreen -f
   ```

4. **Verify Configuration**:
   ```bash
   cat /opt/makerscreen/config.json
   ```

### Content Not Displaying

1. **Check Client Status**: Verify client is "Online" in Management Console
2. **Check Logs**: View client logs for errors
3. **Verify Content Format**: Ensure content is in supported format (PNG, JPG, MP4)
4. **Check Permissions**: Ensure content directory is writable

### Auto-Deploy Not Finding Devices

1. **Check Network Range**: Verify network range in configuration
2. **Enable SSH**: Ensure SSH is enabled on Raspberry Pi
3. **Check Firewall**: Ensure firewall allows network scanning
4. **Verify MAC Address**: Check if Raspberry Pi MAC is recognized

## 🤝 Contributing

We welcome contributions! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License. See LICENSE file for details.

## 🙏 Acknowledgments

- Built with .NET 8 and WPF
- Python WebSocket client using `websockets` library
- Inspired by enterprise digital signage systems

## 📞 Support

For support, please:
- Open an issue on GitHub
- Check the documentation in `/Documentation`
- Review troubleshooting guide above

## 🗺️ Roadmap

### Version 1.1 (Planned)
- [ ] Content scheduling and playlists
- [ ] Multi-zone display layouts
- [x] iOS management app (see `Client/iOS/MakerScreen`)
- [ ] Database backend (SQL Server)
- [ ] Advanced analytics and reporting

### Version 1.2 (Planned)
- [ ] Video content support
- [ ] Interactive web content
- [ ] Touch screen support
- [ ] Emergency broadcast system
- [ ] API for third-party integration

---

**Made with ❤️ for Digital Signage Excellence**
