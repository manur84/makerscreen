# Deployment Guide

This guide covers all deployment scenarios for MakerScreen Digital Signage System.

## Table of Contents

1. [Server Deployment](#server-deployment)
2. [Client Deployment Methods](#client-deployment-methods)
3. [Network Configuration](#network-configuration)
4. [Production Deployment](#production-deployment)
5. [Troubleshooting](#troubleshooting)

## Server Deployment

### Prerequisites

- Windows 10/11 or Windows Server 2019+
- .NET 8 Runtime
- Network connectivity to client subnet
- Static IP address (recommended)

### Installation Steps

#### Option 1: Running from Source

```bash
# Clone repository
git clone https://github.com/yourusername/makerscreen.git
cd makerscreen

# Restore dependencies
cd Server
dotnet restore

# Run Management Console
cd MakerScreen.Management
dotnet run
```

#### Option 2: Published Application

```bash
# Publish release build
cd Server/MakerScreen.Management
dotnet publish -c Release -r win-x64 --self-contained

# Output will be in: bin/Release/net8.0-windows/win-x64/publish/
# Copy to desired location and run MakerScreen.Management.exe
```

### Server Configuration

1. **Configure Network Settings**:
   - Ensure server has static IP
   - Open firewall port 8443 (TCP)
   - Note server IP for client configuration

2. **Windows Firewall Rule**:
   ```powershell
   # Run as Administrator
   New-NetFirewallRule -DisplayName "MakerScreen Server" `
       -Direction Inbound `
       -Protocol TCP `
       -LocalPort 8443 `
       -Action Allow
   ```

3. **Verify Server Running**:
   ```powershell
   # Check if port is listening
   netstat -an | findstr :8443
   ```

## Client Deployment Methods

### Method 1: Auto-Deploy (Recommended)

**Best for**: Deploying to multiple devices quickly

**Prerequisites**:
- Raspberry Pi devices on same network
- SSH enabled on Raspberry Pi
- Default credentials (or configured in script)

**Steps**:

1. **Prepare Raspberry Pi**:
   - Flash Raspberry Pi OS Lite to SD card
   - Enable SSH: Create empty file named `ssh` on boot partition
   - Boot Raspberry Pi and connect to network

2. **Run Auto-Deploy from Management Console**:
   - Open MakerScreen Management Console
   - Click "🚀 Auto-Deploy Clients"
   - System will automatically:
     - Discover Raspberry Pi devices
     - Transfer client files
     - Install dependencies
     - Configure and start service

3. **Verify Deployment**:
   - Check "Connected Clients" panel
   - Clients should appear within 30 seconds

**Manual Script Deployment**:

```bash
# On Windows server
cd Deployment/Scripts
bash auto-deploy.sh

# Edit script to configure:
# - Network range
# - SSH credentials
# - Server URL
```

### Method 2: SD Card Image (Zero-Touch)

**Best for**: Mass deployment, field installations

**Steps**:

1. **Generate Image** (on server):
   ```bash
   cd Deployment/ImageBuilder
   chmod +x build-image.sh
   ./build-image.sh
   ```

2. **Configure Server URL**:
   - Edit `config.json` before building, or
   - Edit on boot partition after flashing

3. **Flash Image**:
   ```bash
   # Using Balena Etcher (GUI)
   # Download from: https://www.balena.io/etcher/
   
   # Or using dd (Linux/Mac)
   sudo dd if=makerscreen-raspios.img.xz of=/dev/sdX bs=4M status=progress
   
   # Or using Win32DiskImager (Windows)
   ```

4. **Configure WiFi** (optional):
   Create `wpa_supplicant.conf` on boot partition:
   ```
   country=US
   ctrl_interface=DIR=/var/run/wpa_supplicant GROUP=netdev
   update_config=1
   
   network={
       ssid="YourNetworkName"
       psk="YourPassword"
       key_mgmt=WPA-PSK
   }
   ```

5. **Boot Raspberry Pi**:
   - Insert SD card
   - Connect to power
   - Client auto-starts and connects

### Method 3: Manual Installation

**Best for**: Single device, custom configuration

**Steps**:

1. **Transfer Files**:
   ```bash
   # From server
   scp -r Client/RaspberryPi/* pi@raspberrypi:/tmp/
   ```

2. **SSH to Raspberry Pi**:
   ```bash
   ssh pi@raspberrypi
   ```

3. **Run Installation**:
   ```bash
   cd /tmp
   chmod +x install.sh
   ./install.sh
   ```

4. **Configure Server URL**:
   ```bash
   sudo nano /opt/makerscreen/config.json
   
   # Edit serverUrl:
   {
     "serverUrl": "ws://192.168.1.100:8443",
     "autoStart": true
   }
   ```

5. **Start Service**:
   ```bash
   sudo systemctl start makerscreen
   sudo systemctl status makerscreen
   ```

## Network Configuration

### Network Topology Options

#### Option 1: Flat Network (Simple)

```
Server: 192.168.1.100
Client 1: 192.168.1.101
Client 2: 192.168.1.102
Client N: 192.168.1.10N

All devices on same subnet
```

**Configuration**:
- No special setup required
- Suitable for small installations (<50 displays)

#### Option 2: VLAN Segmentation (Recommended)

```
Management VLAN (VLAN 10):
  Server: 192.168.10.10

Signage VLAN (VLAN 20):
  Client 1: 192.168.20.11
  Client 2: 192.168.20.12
  Client N: 192.168.20.1N

Router allows VLAN 20 -> VLAN 10 (port 8443 only)
```

**Benefits**:
- Network isolation
- Better security
- Easier troubleshooting
- Scalable to 1000+ devices

**Switch Configuration** (example for Cisco):
```
interface GigabitEthernet1/0/1
  description "MakerScreen Server"
  switchport mode access
  switchport access vlan 10

interface GigabitEthernet1/0/2-48
  description "Signage Displays"
  switchport mode access
  switchport access vlan 20
```

### DNS/mDNS Configuration

**Option 1: Static IP Configuration**
- Assign static IPs to all devices
- Configure clients with server IP

**Option 2: DNS**
- Create DNS entry: `makerscreen-server.local`
- Configure clients with hostname

**Option 3: mDNS** (future enhancement)
- Automatic service discovery
- No configuration required

### Firewall Rules

**Server Firewall**:
```bash
# Allow WebSocket connections
Allow TCP port 8443 from 192.168.20.0/24

# Block all other inbound
Deny all
```

**Client Firewall** (if enabled):
```bash
# Allow outbound to server
Allow TCP port 8443 to 192.168.10.10

# Block all other outbound
Deny all
```

## Production Deployment

### SSL/TLS Configuration

For production, enable WSS (WebSocket Secure):

1. **Generate SSL Certificate**:
   ```powershell
   # Self-signed (development only)
   New-SelfSignedCertificate `
       -DnsName "makerscreen-server.local" `
       -CertStoreLocation "cert:\LocalMachine\My" `
       -KeyExportPolicy Exportable
   
   # Production: Use certificate from CA
   # - Let's Encrypt (free)
   # - DigiCert, Sectigo (commercial)
   ```

2. **Configure Server**:
   ```csharp
   // In SecureWebSocketServer.cs
   _listener.Prefixes.Add($"https://+:{_port}/");
   
   // Add certificate to HttpListener
   // Implementation depends on certificate type
   ```

3. **Update Client Configuration**:
   ```json
   {
     "serverUrl": "wss://192.168.10.10:8443",
     "autoStart": true
   }
   ```

### High Availability

For mission-critical deployments:

1. **Redundant Servers**:
   - Deploy two servers
   - Use load balancer
   - Shared content storage

2. **Database Backend**:
   - SQL Server Always On
   - Content stored in database
   - Automatic failover

3. **Monitoring**:
   - SCOM/Nagios for server monitoring
   - Alert on server failure
   - Automatic client reconnection

### Backup and Recovery

1. **Server Backup**:
   ```powershell
   # Backup content directory
   robocopy "C:\MakerScreen\Content" "\\backup\MakerScreen\Content" /MIR /Z
   
   # Backup configuration
   Copy-Item "C:\MakerScreen\appsettings.json" "\\backup\MakerScreen\"
   ```

2. **Client Image Backup**:
   - Keep master SD card image
   - Document configuration changes
   - Test restore procedure

### Scaling Guidelines

| Clients | Server Specs | Network | Notes |
|---------|--------------|---------|-------|
| 1-50 | 4GB RAM, 2 CPU | 100 Mbps | Single server |
| 51-100 | 8GB RAM, 4 CPU | 1 Gbps | Single server |
| 101-500 | 16GB RAM, 8 CPU | 1 Gbps | Add caching |
| 501-1000 | 32GB RAM, 16 CPU | 10 Gbps | Multiple servers |
| 1000+ | Cluster | 10 Gbps | Microservices |

## Troubleshooting

### Server Issues

**Server won't start**:
```bash
# Check .NET installation
dotnet --version

# Check port availability
netstat -an | findstr :8443

# Check firewall
Get-NetFirewallRule -DisplayName "*MakerScreen*"
```

**No clients connecting**:
```bash
# Verify server is listening
netstat -an | findstr :8443

# Check firewall allows connections
# Test from client network
```

### Client Issues

**Client won't install**:
```bash
# Check SSH connectivity
ssh pi@CLIENT_IP

# Check internet connectivity (for pip install)
ping 8.8.8.8

# Check Python version
python3 --version
```

**Client won't connect**:
```bash
# Check configuration
cat /opt/makerscreen/config.json

# Test server connectivity
telnet SERVER_IP 8443

# Check client logs
sudo journalctl -u makerscreen -f
```

**Client connects but no content**:
```bash
# Check content directory permissions
ls -la /opt/makerscreen/content/

# Check client logs
sudo journalctl -u makerscreen -n 100

# Restart service
sudo systemctl restart makerscreen
```

### Network Issues

**Slow content transfer**:
```bash
# Test network speed
iperf3 -c SERVER_IP

# Check for packet loss
ping -c 100 SERVER_IP

# Verify network infrastructure (switches, routers)
```

**Clients randomly disconnecting**:
```bash
# Check heartbeat interval (should be 30s)
# Check network stability
# Check for DHCP lease expiration
# Consider static IPs
```

### Performance Issues

**Server CPU high**:
```bash
# Check number of connected clients
# Consider scaling to multiple servers
# Enable caching
```

**Memory issues**:
```bash
# Check content size
# Enable content cleanup
# Increase server RAM
```

## Post-Deployment

### Monitoring

1. **Set up monitoring**:
   - Check "Connected Clients" daily
   - Review logs weekly
   - Test content updates monthly

2. **Scheduled maintenance**:
   - Update server software monthly
   - Update client OS quarterly
   - Review security patches

### Documentation

1. **Document your deployment**:
   - Server IP addresses
   - Client locations
   - Network configuration
   - Login credentials (encrypted)

2. **Create runbooks**:
   - Adding new clients
   - Replacing failed clients
   - Updating content
   - Emergency procedures

### Training

1. **Train administrators**:
   - Adding content
   - Deploying clients
   - Troubleshooting
   - Security procedures

2. **Create user guides**:
   - Screenshots of UI
   - Step-by-step procedures
   - Common scenarios

## Next Steps

After successful deployment:

1. ✅ Test content distribution
2. ✅ Verify all clients connected
3. ✅ Test failover scenarios
4. ✅ Document configuration
5. ✅ Train users
6. ✅ Set up monitoring
7. ✅ Schedule maintenance
8. ✅ Plan for scaling

For questions or issues, refer to:
- [README.md](../README.md)
- [ARCHITECTURE.md](./ARCHITECTURE.md)
- GitHub Issues
