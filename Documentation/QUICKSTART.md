# Quick Start Guide

Get MakerScreen up and running in 5 minutes!

## Server Setup (Windows)

1. **Install .NET 8**:
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Install the Desktop Runtime

2. **Clone and Run**:
   ```bash
   git clone https://github.com/yourusername/makerscreen.git
   cd makerscreen/Server/MakerScreen.Management
   dotnet run
   ```

3. **Note Your Server IP**:
   ```powershell
   ipconfig
   # Look for IPv4 Address
   ```

## Client Setup (Raspberry Pi)

### Quick Method: Pre-configured SD Card

1. **Flash Raspberry Pi OS**:
   - Download: https://www.raspberrypi.com/software/
   - Flash to SD card using Raspberry Pi Imager

2. **Enable SSH**:
   - Create empty file named `ssh` in boot partition

3. **Copy Client Files**:
   ```bash
   # From Windows server, use WinSCP or:
   scp -r Client/RaspberryPi/* pi@raspberrypi:/tmp/
   ```

4. **Install on Raspberry Pi**:
   ```bash
   ssh pi@raspberrypi
   cd /tmp
   chmod +x install.sh
   ./install.sh
   ```

5. **Configure Server**:
   ```bash
   sudo nano /opt/makerscreen/config.json
   # Change serverUrl to your server's IP
   # Example: "serverUrl": "ws://192.168.1.100:8443"
   ```

6. **Start Service**:
   ```bash
   sudo systemctl start makerscreen
   ```

## Test the System

1. **Check Server**: 
   - Management Console should show "Server Running"
   
2. **Check Client**:
   ```bash
   sudo systemctl status makerscreen
   # Should show "active (running)"
   ```

3. **Verify Connection**:
   - In Management Console, click "🔄 Refresh"
   - Your Raspberry Pi should appear in "Connected Clients"

4. **Push Content**:
   - Click "➕ Add Content"
   - Select an image
   - Click "Push" to send to display

## Common Issues

**Client not appearing?**
```bash
# Check logs on Raspberry Pi:
sudo journalctl -u makerscreen -f

# Verify server IP is correct:
cat /opt/makerscreen/config.json

# Test connectivity:
ping YOUR_SERVER_IP
```

**Can't connect to Raspberry Pi?**
```bash
# Find Raspberry Pi IP:
# On Windows:
arp -a

# Or check your router's DHCP table
```

## Next Steps

- Read [README.md](../README.md) for full features
- Review [DEPLOYMENT.md](./DEPLOYMENT.md) for production setup
- Check [ARCHITECTURE.md](./ARCHITECTURE.md) for technical details

## Auto-Deploy (Advanced)

Once you have one client working:

1. Flash multiple SD cards with Raspberry Pi OS
2. Enable SSH on all
3. Boot all Raspberry Pis
4. Click "🚀 Auto-Deploy Clients" in Management Console
5. System automatically installs on all discovered devices!

---

**That's it! You now have a working digital signage system.**
