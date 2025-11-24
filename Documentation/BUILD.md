# Build Guide

Complete build instructions for MakerScreen Digital Signage Management System.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Development Environment Setup](#development-environment-setup)
3. [Building Server Components](#building-server-components)
4. [Building Client Components](#building-client-components)
5. [Building Deployment Tools](#building-deployment-tools)
6. [Testing](#testing)
7. [Creating Release Builds](#creating-release-builds)
8. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software

#### For Server Development (Windows)

- [ ] **Operating System**: Windows 10/11 or Windows Server 2019+
- [ ] **.NET 8 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
  - Version: 8.0.0 or later
  - Verify installation: `dotnet --version`
- [ ] **Git**: [Download](https://git-scm.com/downloads)
  - Version: 2.30 or later
- [ ] **Visual Studio 2022** (recommended) or **Visual Studio Code**
  - Workloads (if using VS 2022):
    - .NET desktop development
    - .NET Core cross-platform development

#### For Client Development (Raspberry Pi/Linux)

- [ ] **Python**: 3.11 or later
  - Verify installation: `python3 --version`
- [ ] **pip**: Python package installer
  - Verify installation: `pip3 --version`
- [ ] **Virtual Environment** (recommended): `python3-venv`

#### Optional Tools

- [ ] **Windows Terminal**: Enhanced command-line experience
- [ ] **PowerShell 7+**: For advanced scripting
- [ ] **Docker**: For containerized builds (optional)

### Hardware Requirements

#### Server Development Machine
- **CPU**: Dual-core or better
- **RAM**: 8 GB minimum, 16 GB recommended
- **Storage**: 10 GB free space
- **Network**: Ethernet connection (for testing with clients)

#### Client Development/Testing
- **Raspberry Pi**: Model 3B+ or later
- **SD Card**: 16 GB minimum, Class 10
- **Display**: HDMI-compatible monitor/TV

## Development Environment Setup

### Clone the Repository

```bash
# Clone repository
git clone https://github.com/yourusername/makerscreen.git
cd makerscreen

# Verify directory structure
ls -la
```

**Expected Structure**:
```
makerscreen/
├── Client/
│   └── RaspberryPi/
├── Deployment/
│   ├── ImageBuilder/
│   └── Scripts/
├── Documentation/
├── Server/
│   ├── MakerScreen.Core/
│   ├── MakerScreen.Management/
│   ├── MakerScreen.Server/
│   └── MakerScreen.Services/
├── MakerScreen.sln
└── README.md
```

### Verify Prerequisites

```bash
# Check .NET SDK
dotnet --version
# Expected: 8.0.x or later

# Check .NET workloads
dotnet workload list

# Check Python (on Linux/Pi)
python3 --version
# Expected: 3.11.x or later

# Check Git
git --version
# Expected: 2.30.x or later
```

## Building Server Components

### Restore Dependencies

```bash
# Navigate to solution directory
cd Server

# Restore all NuGet packages
dotnet restore

# Alternative: Restore specific project
dotnet restore MakerScreen.Management/MakerScreen.Management.csproj
```

**Expected Output**:
```
Restore completed in 3.45 sec for MakerScreen.Core.csproj.
Restore completed in 4.12 sec for MakerScreen.Services.csproj.
Restore completed in 4.89 sec for MakerScreen.Server.csproj.
Restore completed in 5.21 sec for MakerScreen.Management.csproj.
```

### Build All Projects

#### Debug Build (Development)

```bash
# From solution root
dotnet build MakerScreen.sln -c Debug

# Or from Server directory
cd Server
dotnet build -c Debug
```

**Verification Checklist**:
- [ ] MakerScreen.Core builds without errors
- [ ] MakerScreen.Services builds without errors
- [ ] MakerScreen.Server builds without errors
- [ ] MakerScreen.Management builds without errors
- [ ] No warnings about missing dependencies

#### Build Individual Projects

```bash
# Core library
cd Server/MakerScreen.Core
dotnet build -c Debug

# Services layer
cd ../MakerScreen.Services
dotnet build -c Debug

# Console server
cd ../MakerScreen.Server
dotnet build -c Debug

# WPF Management UI
cd ../MakerScreen.Management
dotnet build -c Debug
```

### Build Output Locations

**Debug Builds**:
```
Server/MakerScreen.Core/bin/Debug/net8.0/
Server/MakerScreen.Services/bin/Debug/net8.0/
Server/MakerScreen.Server/bin/Debug/net8.0/
Server/MakerScreen.Management/bin/Debug/net8.0-windows/
```

**Common Build Artifacts**:
- `.dll` files: Compiled assemblies
- `.pdb` files: Debug symbols
- `.deps.json`: Dependency information
- `.runtimeconfig.json`: Runtime configuration

### Running from Source

#### Management Application (WPF)

```bash
cd Server/MakerScreen.Management
dotnet run
```

**Startup Checklist**:
- [ ] Application window appears
- [ ] No console errors about missing dependencies
- [ ] WebSocket server initializes (check console output)
- [ ] UI is responsive

#### Console Server

```bash
cd Server/MakerScreen.Server
dotnet run
```

**Expected Console Output**:
```
info: MakerScreen.Server[0]
      Starting MakerScreen Server...
info: SecureWebSocketServer[0]
      WebSocket server listening on wss://0.0.0.0:8443
```

## Building Client Components

### Python Client (Raspberry Pi)

#### Create Virtual Environment (Recommended)

```bash
# Navigate to client directory
cd Client/RaspberryPi

# Create virtual environment
python3 -m venv venv

# Activate virtual environment
# On Linux/Raspberry Pi:
source venv/bin/activate
# On Windows (if testing):
venv\Scripts\activate
```

#### Install Dependencies

```bash
# With virtual environment activated
pip install -r requirements.txt

# Verify installation
pip list
```

**Expected Packages**:
- `websockets>=12.0`
- `pillow>=10.1.0`
- `requests>=2.31.0`

**Verification Checklist**:
- [ ] All packages installed without errors
- [ ] No dependency conflicts reported
- [ ] Packages compatible with Python version

#### Test Client (Without Installation)

```bash
# Run client directly (requires server running)
python3 client.py --server-ip <YOUR_SERVER_IP>

# Example:
python3 client.py --server-ip 192.168.1.100
```

**Expected Output**:
```
[INFO] MakerScreen Client starting...
[INFO] Connecting to wss://192.168.1.100:8443
[INFO] Connected to server
[INFO] Registration successful
```

#### Validate Client Installation Package

```bash
# Check install script
chmod +x install.sh
./install.sh --dry-run  # If supported, otherwise inspect script

# Verify systemd service file exists
ls -l makerscreen.service

# Verify requirements file
cat requirements.txt
```

## Building Deployment Tools

### Image Builder

The image builder creates pre-configured Raspberry Pi SD card images.

#### Prerequisites

```bash
# Install required tools (Linux/WSL)
sudo apt-get update
sudo apt-get install -y \
  qemu-user-static \
  kpartx \
  parted \
  wget \
  zip

# Verify installations
which qemu-arm-static
which kpartx
```

#### Build Custom Image

```bash
cd Deployment/ImageBuilder

# Make script executable
chmod +x build-image.sh

# Run image builder
sudo ./build-image.sh

# Optional: Specify parameters
sudo ./build-image.sh --server-ip 192.168.1.100 --output custom-image.img
```

**Build Process**:
1. Downloads base Raspberry Pi OS
2. Mounts image partitions
3. Installs Python dependencies
4. Copies client files
5. Configures systemd service
6. Sets server connection details
7. Unmounts and compresses image

**Expected Output**:
```
[INFO] Downloading Raspberry Pi OS...
[INFO] Extracting image...
[INFO] Mounting partitions...
[INFO] Installing dependencies...
[INFO] Configuring client...
[INFO] Creating systemd service...
[INFO] Cleaning up...
[INFO] Image created: makerscreen-image.img
```

**Verification Checklist**:
- [ ] Image file created successfully
- [ ] Image size is reasonable (2-4 GB compressed)
- [ ] No errors during build process
- [ ] Checksums match (if provided)

### Auto-Deployment Scripts

```bash
cd Deployment/Scripts

# Make script executable
chmod +x auto-deploy.sh

# Test script syntax
bash -n auto-deploy.sh

# Run deployment (requires SSH access to Pi)
./auto-deploy.sh --target pi@raspberrypi --server-ip 192.168.1.100
```

**Script Functionality**:
- Detects Raspberry Pi devices on network
- Copies client files via SSH/SCP
- Runs installation remotely
- Configures and starts service
- Verifies successful deployment

## Testing

### Unit Tests

> **Note**: Add unit test projects as development progresses.

```bash
# Run all tests
dotnet test MakerScreen.sln

# Run tests with coverage
dotnet test MakerScreen.sln --collect:"XPlat Code Coverage"

# Run tests for specific project
dotnet test Server/MakerScreen.Core.Tests/
```

### Integration Tests

#### Server-Client Communication

**Setup**:
1. Start server application
2. Note server IP address
3. Start Python client with server IP

**Test Checklist**:
- [ ] Client connects to server successfully
- [ ] Client registers with server
- [ ] Server shows client in connected devices list
- [ ] Heartbeat messages received
- [ ] Content can be sent to client
- [ ] Client displays content correctly

#### Manual Testing Procedure

```bash
# Terminal 1: Start server
cd Server/MakerScreen.Management
dotnet run

# Terminal 2: Start client (on Pi or test machine)
cd Client/RaspberryPi
source venv/bin/activate
python3 client.py --server-ip 192.168.1.100

# Terminal 3: Monitor logs
tail -f /var/log/makerscreen.log  # On Pi
```

**Test Scenarios**:
1. **Connection Test**: Verify client connects and registers
2. **Content Delivery**: Send image/video from server
3. **Disconnect/Reconnect**: Test resilience
4. **Multiple Clients**: Connect 2+ clients simultaneously
5. **Network Interruption**: Unplug/replug network cable

### Build Verification

```bash
# Verify all projects build in clean environment
git clean -xdf
dotnet restore
dotnet build -c Release

# Check for warnings
dotnet build -c Release /warnaserror
```

**Success Criteria**:
- [ ] All projects build without errors
- [ ] No critical warnings
- [ ] All dependencies resolved
- [ ] Output assemblies created

## Creating Release Builds

### Server Applications

#### Self-Contained Deployment

Includes .NET runtime with application (recommended for production).

```bash
# Build Management Application
cd Server/MakerScreen.Management
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Output location:
# bin/Release/net8.0-windows/win-x64/publish/
```

**Publish Options Explained**:
- `-c Release`: Release configuration (optimized)
- `-r win-x64`: Target Windows x64 platform
- `--self-contained`: Include .NET runtime
- `-p:PublishSingleFile=true`: Single executable file

**Other Platform Targets**:
```bash
# Windows ARM64
dotnet publish -c Release -r win-arm64 --self-contained

# Linux x64 (for server on Linux)
dotnet publish -c Release -r linux-x64 --self-contained

# Framework-dependent (requires .NET 8 installed)
dotnet publish -c Release -r win-x64 --self-contained false
```

#### Console Server Application

```bash
cd Server/MakerScreen.Server
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Output location:
# bin/Release/net8.0/win-x64/publish/
```

### Creating Installer Package

#### Using Visual Studio Installer Project

1. Open `MakerScreen.sln` in Visual Studio 2022
2. Add new project → Setup Project
3. Configure:
   - Primary output from MakerScreen.Management
   - Include dependencies
   - Set installation folder
   - Configure registry entries (if needed)
4. Build installer

#### Using WiX Toolset (Advanced)

```bash
# Install WiX Toolset
dotnet tool install --global wix

# Create installer
cd Deployment/Installer
wix build MakerScreen.wxs -o MakerScreen-Setup.msi
```

### Client Distribution Package

```bash
# Create deployment package
cd Client/RaspberryPi

# Create archive with all necessary files
tar -czf makerscreen-client.tar.gz \
  client.py \
  install.sh \
  makerscreen.service \
  requirements.txt

# Verify archive
tar -tzf makerscreen-client.tar.gz
```

### Release Checklist

- [ ] Version numbers updated in all project files
- [ ] Release notes prepared
- [ ] All tests passing
- [ ] Documentation updated
- [ ] Server applications published (Windows x64)
- [ ] Client package created
- [ ] Deployment image built (if applicable)
- [ ] Installer tested on clean Windows machine
- [ ] Client tested on clean Raspberry Pi
- [ ] GitHub release created with artifacts

### Versioning

Update version in `.csproj` files:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

### Creating Release Archive

```bash
# Create comprehensive release package
mkdir -p releases/v1.0.0

# Copy server binaries
cp -r Server/MakerScreen.Management/bin/Release/net8.0-windows/win-x64/publish releases/v1.0.0/Server

# Copy client files
cp -r Client/RaspberryPi releases/v1.0.0/Client

# Copy documentation
cp -r Documentation releases/v1.0.0/

# Create archive
cd releases
zip -r makerscreen-v1.0.0.zip v1.0.0/
```

## Troubleshooting

### Common Build Issues

#### .NET SDK Not Found

**Error**:
```
error: The command could not be loaded, possibly because:
  * You intended to execute a .NET application
```

**Solution**:
```bash
# Verify .NET installation
dotnet --info

# If not installed, download from:
# https://dotnet.microsoft.com/download/dotnet/8.0

# Add to PATH (Windows PowerShell)
$env:PATH += ";C:\Program Files\dotnet"

# Add to PATH (Linux/Mac)
export PATH=$PATH:/usr/share/dotnet
```

#### NuGet Package Restore Failed

**Error**:
```
error NU1301: Unable to load the service index for source
```

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Retry restore with verbose logging
dotnet restore -v detailed

# Use alternative package source
dotnet restore --source https://api.nuget.org/v3/index.json
```

#### WPF Build Errors

**Error**:
```
error: The current .NET SDK does not support 'net8.0-windows' as a target framework
```

**Solution**:
```bash
# Verify .NET SDK version
dotnet --version
# Must be 8.0.0 or later

# Update SDK if needed
# Download latest from dotnet.microsoft.com

# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### Missing Dependencies in Python Client

**Error**:
```
ModuleNotFoundError: No module named 'websockets'
```

**Solution**:
```bash
# Ensure virtual environment is activated
source venv/bin/activate

# Upgrade pip
pip install --upgrade pip

# Reinstall dependencies
pip install -r requirements.txt --force-reinstall

# On Raspberry Pi, if compilation fails:
sudo apt-get install python3-dev libffi-dev
pip install -r requirements.txt
```

#### Build Fails with "Out of Memory"

**Solution**:
```bash
# Limit concurrent builds
dotnet build -maxcpucount:1

# Or set environment variable
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Increase available memory (Visual Studio)
# Tools > Options > Projects and Solutions > Build and Run
# Set "maximum number of parallel project builds" to 1
```

#### Permission Denied (Linux/Mac)

**Error**:
```
bash: ./build-image.sh: Permission denied
```

**Solution**:
```bash
# Make script executable
chmod +x build-image.sh

# Or run with bash explicitly
bash build-image.sh
```

### Raspberry Pi Image Build Issues

#### Insufficient Disk Space

**Error**:
```
dd: error writing: No space left on device
```

**Solution**:
```bash
# Check available space
df -h

# Clean up old builds
rm -rf /tmp/makerscreen-*

# Specify smaller image size in build script
```

#### qemu-user-static Not Found

**Error**:
```
/usr/bin/qemu-arm-static: No such file or directory
```

**Solution**:
```bash
# Install QEMU
sudo apt-get update
sudo apt-get install -y qemu-user-static

# Verify installation
which qemu-arm-static
```

#### Mount Failed

**Error**:
```
mount: /mnt/rpi-image: failed to setup loop device
```

**Solution**:
```bash
# Load loop kernel module
sudo modprobe loop

# Increase max loop devices
echo "options loop max_loop=64" | sudo tee /etc/modprobe.d/loop.conf

# Reboot or reload module
sudo rmmod loop
sudo modprobe loop

# Ensure script runs with sudo
sudo ./build-image.sh
```

### Network and Firewall Issues

#### WebSocket Connection Refused

**Problem**: Client cannot connect to server.

**Solution**:
```powershell
# Windows: Allow through firewall
New-NetFirewallRule -DisplayName "MakerScreen WebSocket" `
  -Direction Inbound -LocalPort 8443 -Protocol TCP -Action Allow

# Verify server is listening
netstat -an | findstr 8443

# Linux: Allow through firewall
sudo ufw allow 8443/tcp
sudo iptables -I INPUT -p tcp --dport 8443 -j ACCEPT
```

#### SSL/TLS Certificate Errors

**Error**:
```
SSL handshake failed: certificate verify failed
```

**Solution**:
```bash
# For development, update client to accept self-signed certificates
# In client.py, when creating SSL context:
ssl_context.check_hostname = False
ssl_context.verify_mode = ssl.CERT_NONE

# For production, use proper certificates
# See DEPLOYMENT.md for certificate setup
```

### Build Performance Optimization

```bash
# Enable parallel builds
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet build -c Release -m

# Use local NuGet cache
dotnet nuget locals all --list

# Skip unnecessary steps
dotnet build --no-restore --no-dependencies
```

## Additional Resources

- **Architecture**: See [ARCHITECTURE.md](ARCHITECTURE.md) for system design details
- **Deployment**: See [DEPLOYMENT.md](DEPLOYMENT.md) for installation and configuration
- **Quick Start**: See [QUICKSTART.md](QUICKSTART.md) for rapid setup
- **.NET Documentation**: https://docs.microsoft.com/dotnet/
- **Python Documentation**: https://docs.python.org/3/
- **Raspberry Pi Documentation**: https://www.raspberrypi.org/documentation/

## Getting Help

If you encounter issues not covered in this guide:

1. Check existing GitHub Issues
2. Review application logs:
   - Server: Console output or Event Viewer (Windows)
   - Client: `/var/log/makerscreen.log` (Raspberry Pi)
3. Enable verbose logging and retry
4. Create a GitHub Issue with:
   - Error messages
   - Build environment details
   - Steps to reproduce

---

**Last Updated**: 2025-11-24  
**Version**: 1.0.0
