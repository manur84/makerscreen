# MakerScreen Deployment Guide

## Quick Start

This guide provides step-by-step instructions for deploying MakerScreen in your environment, from initial server installation to managing 100+ displays.

**Total Deployment Time**: < 1 hour for server + 3 minutes per client

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Server Installation](#server-installation)
3. [Client Deployment](#client-deployment)
4. [iOS App Setup](#ios-app-setup)
5. [First Content Deployment](#first-content-deployment)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Server Requirements

**Hardware:**
- CPU: 4 cores @ 2.5 GHz or higher
- RAM: 16 GB minimum (32 GB recommended for 100+ clients)
- Storage: 500 GB SSD (OS + Content)
- Network: 1 Gbps NIC

**Software:**
- Windows Server 2019/2022 (or Windows 10/11 Pro)
- .NET 8 Runtime (included in installer)
- SQL Server 2019+ (Express included, or use existing instance)

**Network:**
- Static IP address or reserved DHCP
- Firewall rules for ports 8443, 8080, 5353
- mDNS enabled on network

### Client Requirements (Raspberry Pi)

**Hardware:**
- Raspberry Pi 4 Model B (4GB RAM recommended)
- microSD card (32 GB Class 10 or better)
- Power supply (5V 3A USB-C)
- HDMI cable
- Display/TV with HDMI input

**Network:**
- Ethernet connection (recommended) or WiFi
- Access to same network as server
- DHCP or static IP configuration

### iOS Requirements

**Device:**
- iPhone or iPad running iOS 15.0 or later
- Face ID / Touch ID enabled (recommended)

**Network:**
- Access to server network (WiFi or VPN)

---

## Server Installation

### Method 1: One-Click Installer (Recommended)

**Step 1: Download Installer**
```
Download: MakerScreenServer-Setup-v1.0.exe
Size: ~500 MB (includes all dependencies)
SHA256: [checksum]
```

**Step 2: Run Installer**

1. Right-click installer → "Run as Administrator"
2. Accept UAC prompt
3. Choose installation directory (default: `C:\Program Files\MakerScreen`)
4. Click "Install"

**Step 3: Initial Configuration Wizard**

The wizard will guide you through:

1. **Database Setup**
   - Option A: Install SQL Server Express (recommended for new deployments)
   - Option B: Connect to existing SQL Server
   
   ```
   [√] Install SQL Server Express 2022
   [ ] Use existing SQL Server
       Server: _________________
       Database: MakerScreen_____
       Authentication: [Windows Auth ▼]
   ```

2. **Admin Account Creation**
   ```
   Username: _______________
   Email: __________________
   Password: ***************
   Confirm: ****************
   ```

3. **Network Configuration**
   ```
   Server IP: 10.0.1.10________ [Auto-detected]
   WebSocket Port: 8443________ [Default]
   API Port: 8080______________ [Default]
   
   [√] Create firewall rules automatically
   [√] Enable mDNS discovery
   ```

4. **Certificate Generation**
   ```
   Generating certificates...
   [████████████████████] 100%
   
   √ Root CA created
   √ Intermediate CA created  
   √ Server certificate generated
   √ Certificates installed
   ```

5. **Service Installation**
   ```
   Installing Windows Services...
   
   √ MakerScreen Server Service
   √ WebSocket Server
   √ Auto-Discovery Service
   √ Certificate Management Service
   
   Services configured to start automatically.
   ```

**Step 4: Verification**

The installer will perform health checks:

```
Running health checks...

√ Database connection: OK
√ WebSocket server: Listening on port 8443
√ API server: Listening on port 8080
√ mDNS service: Broadcasting
√ Certificates: Valid (expires 2026-01-15)
√ Firewall rules: Configured

Installation complete!

Management Application: C:\Program Files\MakerScreen\MakerScreen.exe
Documentation: C:\Program Files\MakerScreen\docs\
```

**Step 5: Launch Management Application**

1. Double-click `MakerScreen.exe` or use Start Menu
2. Login with admin credentials created in wizard
3. You'll see the main dashboard:

```
┌─────────────────────────────────────────────┐
│ MakerScreen Management Console              │
├─────────────────────────────────────────────┤
│                                             │
│  System Status: ● Running                   │
│  Clients: 0 online, 0 total                │
│  Content: 0 items                          │
│  Playlists: 0 active                       │
│                                             │
│  Quick Actions:                             │
│  ┌──────────────────┐ ┌──────────────────┐ │
│  │ Upload Content   │ │ Create Client    │ │
│  │                  │ │ Image            │ │
│  └──────────────────┘ └──────────────────┘ │
│                                             │
└─────────────────────────────────────────────┘
```

**Total Time**: < 5 minutes

### Method 2: Manual Installation

See [MANUAL_INSTALLATION.md](./MANUAL_INSTALLATION.md) for advanced scenarios.

---

## Client Deployment

### Method 1: Automated Image Builder (Recommended)

**Step 1: Create Client Image**

1. In Management Application, go to **Deployment** → **Image Builder**

2. Configure image settings:
   ```
   ┌─────────────────────────────────────────┐
   │ Raspberry Pi Image Builder              │
   ├─────────────────────────────────────────┤
   │                                         │
   │ Base Image:                             │
   │ [Raspberry Pi OS Lite (64-bit) ▼]       │
   │                                         │
   │ Network Configuration:                  │
   │ ( ) DHCP                                │
   │ (√) WiFi                                │
   │     SSID: CompanyWiFi______________     │
   │     Password: *********************    │
   │ ( ) Static IP                           │
   │                                         │
   │ Hostname Pattern:                       │
   │ [display-{location}-{number}]           │
   │                                         │
   │ Server Connection:                      │
   │ [√] Auto-discover via mDNS              │
   │ [ ] Manual (IP: ______________)         │
   │                                         │
   │ Options:                                │
   │ [√] Embed certificates                  │
   │ [√] Pre-configure SSH keys              │
   │ [√] Enable auto-updates                 │
   │                                         │
   │     [Build Image]                       │
   └─────────────────────────────────────────┘
   ```

3. Click **Build Image**

4. Progress indicator:
   ```
   Building Raspberry Pi Image...
   
   [√] Downloading base image (450 MB)
   [√] Mounting image
   [√] Installing system updates
   [√] Installing Python 3.11
   [√] Installing MakerScreen client
   [√] Configuring network
   [√] Embedding certificates
   [√] Configuring services
   [√] Unmounting image
   [√] Compressing image
   
   Image created: MakerScreen-Client-v1.0.img.gz
   Size: 1.2 GB (compressed)
   SHA256: a3f4b2...
   
   [Download Image] [Write to SD Card]
   ```

**Step 2: Write Image to SD Card**

**Option A: Built-in SD Card Writer**

1. Insert SD card into server PC
2. Click **Write to SD Card**
3. Select SD card from list
4. Confirm overwrite warning
5. Wait for write and verification (2-3 minutes)
6. Eject SD card when complete

**Option B: External Tools**

1. Click **Download Image**
2. Use Balena Etcher or Win32 Disk Imager:
   ```
   balenaEtcher:
   1. Select MakerScreen-Client-v1.0.img.gz
   2. Select SD card
   3. Flash!
   ```

**Step 3: Deploy Raspberry Pi**

1. Insert SD card into Raspberry Pi
2. Connect HDMI to display
3. Connect Ethernet (or rely on WiFi pre-configured)
4. Connect power supply

**Step 4: First Boot**

The Pi will automatically:

```
Boot Sequence:
[  0s] Power on
[  5s] Raspberry Pi OS boots
[ 15s] Network configuration
[ 20s] Server discovery via mDNS
[ 25s] Certificate validation
[ 30s] Client registration
[ 35s] Download initial content (if available)
[ 40s] Start display application

If no content available:
[ 45s] Display status screen with QR code
```

**Step 5: Approve Client (if not auto-approved)**

In Management Application:

1. You'll see notification: "New client pending approval"
2. Go to **Clients** → **Pending**
3. Review client details:
   ```
   Hostname: display-lobby-01
   MAC: b8:27:eb:12:34:56
   IP: 10.0.2.15
   Model: Raspberry Pi 4 Model B (4GB)
   First Seen: 2024-01-15 10:30:45
   
   [Approve] [Reject]
   ```
4. Click **Approve**
5. Client immediately connects and is ready

**Total Time**: 3 minutes per client (after image is built)

### Method 2: Network Boot (PXE)

For large deployments, network boot eliminates SD card writing:

1. Enable PXE boot server in Management Application
2. Configure Raspberry Pi for network boot (one-time setup)
3. Connect Pi to network and power
4. Pi boots from network and auto-configures

See [PXE_BOOT_GUIDE.md](./PXE_BOOT_GUIDE.md) for details.

---

## iOS App Setup

### Step 1: Install App

**Option A: App Store** (if published)
1. Open App Store
2. Search "MakerScreen"
3. Tap Install

**Option B: TestFlight** (beta testing)
1. Install TestFlight from App Store
2. Open invitation link
3. Tap Install

**Option C: Enterprise Distribution** (internal deployment)
1. Open enterprise distribution link
2. Tap Install
3. Trust enterprise certificate in Settings

### Step 2: Connect to Server

**Method A: QR Code (Easiest)**

1. Open MakerScreen app
2. Tap "Scan QR Code"
3. In Management Application, go to **Settings** → **Mobile Access**
4. Click "Show QR Code"
5. Scan QR code with iPhone

The QR code contains:
```json
{
  "server": "wss://10.0.1.10:8443",
  "ca_cert": "-----BEGIN CERTIFICATE-----\n...",
  "name": "Production Server"
}
```

**Method B: Manual Setup**

1. Open MakerScreen app
2. Tap "Manual Setup"
3. Enter server details:
   ```
   Server Address: 10.0.1.10
   Port: 8443
   Use TLS: [√]
   
   [Connect]
   ```

### Step 3: Login

```
┌─────────────────────────────┐
│ MakerScreen                 │
├─────────────────────────────┤
│                             │
│ Username: ______________    │
│ Password: **************    │
│                             │
│ [√] Remember me             │
│ [√] Enable Face ID          │
│                             │
│     [Login]                 │
│                             │
│ Forgot password?            │
└─────────────────────────────┘
```

### Step 4: Enable Notifications

```
Allow "MakerScreen" to send you notifications?

Notifications may include alerts about:
• Client offline events
• Content updates
• System errors
• Scheduled tasks

[Don't Allow] [Allow]
```

Tap **Allow** for full functionality.

**Total Time**: < 1 minute

---

## First Content Deployment

### Step 1: Upload Content

**In Windows Application:**

1. Go to **Content** → **Upload**
2. Drag & drop PNG files (or click to browse)
3. Files are processed automatically:
   ```
   Uploading content...
   
   lobby-welcome.png
   [████████████████████] 100% - 2.5 MB
   √ Uploaded
   √ Thumbnail generated
   √ Optimized
   
   cafeteria-menu.png
   [████████████████████] 100% - 3.1 MB
   √ Uploaded
   √ Thumbnail generated
   √ Optimized
   
   Upload complete. 2 files added.
   ```

4. Edit metadata if desired:
   ```
   Title: Lobby Welcome Screen
   Tags: welcome, lobby, main
   Category: Signage
   ```

**In iOS App:**

1. Tap **+** button
2. Choose source:
   - Photo Library
   - Take Photo
   - Files
3. Select image(s)
4. Tap **Upload**

### Step 2: Create Playlist

1. Go to **Playlists** → **Create New**
2. Configure playlist:
   ```
   Name: Lobby Rotation___________
   Loop: [√]
   Random: [ ]
   Transition: [Fade ▼] (1.0 seconds)
   Default Duration: [30] seconds
   ```

3. Add content items:
   ```
   Playlist Items:
   1. ☰ lobby-welcome.png (30s)
   2. ☰ cafeteria-menu.png (45s)
   3. ☰ upcoming-events.png (30s)
   
   [Add Content]
   ```

4. Drag to reorder if needed
5. Click **Save**

### Step 3: Assign to Clients

1. Go to **Clients** → select client(s)
2. Right-click → **Assign Playlist**
3. Choose playlist: `Lobby Rotation`
4. Click **Apply**

Or use scheduling for time-based content:

1. Go to **Schedules** → **Create New**
2. Configure:
   ```
   Name: Lobby Schedule__________
   Playlist: [Lobby Rotation ▼]
   
   Time Range:
   Start: 2024-01-15 08:00
   End: 2024-01-15 22:00
   
   Recurrence: [Daily ▼]
   Days: [√] Mon [√] Tue [√] Wed [√] Thu [√] Fri
          [ ] Sat [ ] Sun
   
   Assign to:
   [√] Client: display-lobby-01
   [√] Group: Lobby Displays
   ```
3. Click **Create Schedule**

### Step 4: Verify Deployment

Content deploys automatically within seconds.

**On Display:**
- Content starts playing immediately
- Transitions between items as configured

**In Management App:**
```
Clients → display-lobby-01 → Status

Current Content: lobby-welcome.png
Playlist: Lobby Rotation
Status: Playing (Item 1 of 3)
Time Remaining: 18 seconds

[Capture Screenshot] [View Logs]
```

**In iOS App:**
- Live preview shows current content
- Status indicator: ● Playing

**Total Time**: < 5 minutes

---

## Adding Dynamic Overlays

### Step 1: Configure Data Source

1. Go to **Data Sources** → **Add New**
2. Choose type: **SQL Server**
3. Configure connection:
   ```
   Name: Sales Database__________
   Server: sql.company.local____
   Database: Sales______________
   Authentication: [Windows Auth ▼]
   
   [Test Connection] → √ Success
   
   [Save]
   ```

### Step 2: Create Data Query

1. Go to **Data Queries** → **Add New**
2. Configure query:
   ```
   Name: Daily Revenue__________
   Data Source: [Sales Database ▼]
   Query Type: [Stored Procedure ▼]
   
   Procedure: sp_GetDailyRevenue
   Parameters:
     @Date: [TODAY]
   
   Refresh Interval: [300] seconds
   
   [Test Query] → Shows sample results
   
   [Save]
   ```

### Step 3: Add Overlay to Content

1. Go to **Content** → select content
2. Click **Edit Overlays**
3. Visual designer opens
4. Click **Add Overlay** → **Text**
5. Draw rectangle on canvas where overlay should appear
6. Configure overlay:
   ```
   Overlay Type: [Text ▼]
   Data Binding: [Daily Revenue ▼]
   Field: [TotalRevenue]
   
   Format: €{value:N2}
   
   Font: [Arial ▼] [48] pts
   Color: [#00FF00] (green)
   Background: [#000000CC] (semi-transparent black)
   Alignment: [Right ▼]
   
   Animation: [Fade In ▼] (500ms)
   
   [Apply]
   ```

6. Overlay updates in real-time on clients!

---

## Troubleshooting

### Common Issues

#### 1. Client Not Appearing in Dashboard

**Symptoms:**
- Client powered on but not visible in management app
- Status screen shows "Searching for server..."

**Solutions:**

**Check 1: Network Connectivity**
```bash
# On Raspberry Pi (via keyboard or SSH)
ping 10.0.1.10  # Server IP
```
If ping fails, check network cable or WiFi configuration.

**Check 2: mDNS Service**
```powershell
# On Windows Server
Get-Service "MakerScreen Discovery"
# Should show: Running

# If stopped:
Start-Service "MakerScreen Discovery"
```

**Check 3: Firewall**
```powershell
# Check if ports are open
Test-NetConnection -ComputerName localhost -Port 8443
Test-NetConnection -ComputerName localhost -Port 5353
```

**Check 4: Client Web UI**
1. Note client IP from status screen (e.g., 10.0.2.15)
2. Open browser: `http://10.0.2.15:8080`
3. Check diagnostics page
4. View connection logs

#### 2. Certificate Errors

**Symptoms:**
- "Certificate validation failed"
- Client connects but immediately disconnects

**Solutions:**

**Check 1: Certificate Expiry**
```
Management App → Settings → Certificates

Server Certificate: Valid until 2026-01-15 ✓
Client Certificates: 15 valid, 0 expired
```

**Check 2: Regenerate Client Certificate**
1. Go to **Clients** → select client
2. Click **Security** → **Regenerate Certificate**
3. Certificate automatically pushed to client
4. Client reconnects

**Check 3: Clock Synchronization**
```bash
# On Raspberry Pi
timedatectl
# Ensure time is correct
```

If time is wrong:
```bash
sudo timedatectl set-ntp true
```

#### 3. Content Not Displaying

**Symptoms:**
- Display shows status screen instead of content
- Status: "Connected, no content"

**Solutions:**

**Check 1: Content Assignment**
```
Clients → display-lobby-01 → Assigned Content

Current: None
Scheduled: None

→ Need to assign playlist or schedule
```

**Check 2: Schedule Timing**
```
Schedules → Lobby Schedule

Active: No (outside time range)
Current Time: 2024-01-15 23:00
Schedule Time: 08:00 - 22:00

→ Schedule not active at this time
```

**Check 3: Content Download**
```
Clients → display-lobby-01 → Logs

ERROR: Failed to download content
Reason: Network timeout

→ Check network bandwidth/stability
```

#### 4. Poor Performance / Lag

**Symptoms:**
- Low FPS (< 30)
- Choppy animations
- High CPU/temperature

**Solutions:**

**Check 1: Client Metrics**
```
Clients → display-lobby-01 → Performance

CPU: 85% (High!)
RAM: 850 MB / 4 GB
Temperature: 68°C
FPS: 24

→ Performance issue detected
```

**Check 2: Resolution Match**
```
Display: 3840x2160 (4K)
Content: 1920x1080 (FHD)

→ Content being upscaled (CPU intensive)

Solution: Upload content in native resolution
```

**Check 3: Overlay Complexity**
```
Active Overlays: 15
Refresh Rate: Every 5 seconds

→ Too many overlays

Solution: Reduce overlay count or increase refresh interval
```

**Check 4: Hardware Acceleration**
```bash
# Via client web UI or SSH
vcgencmd get_config int | grep gpu_mem
# Should show: gpu_mem=256 or higher
```

If low, increase GPU memory allocation.

#### 5. iOS App Won't Connect

**Symptoms:**
- "Unable to connect to server"
- Certificate errors

**Solutions:**

**Check 1: Network Access**
- Ensure iPhone on same network as server
- Or connected via VPN

**Check 2: Certificate Installation**
1. Management App → **Settings** → **Mobile Access**
2. **Export iOS Certificate**
3. AirDrop to iPhone
4. iPhone Settings → **Profile Downloaded**
5. Install profile
6. Settings → **General** → **About** → **Certificate Trust Settings**
7. Enable trust for MakerScreen CA

**Check 3: Firewall (if remote)**
```
VPN Connected: ✓
Can ping server: ✗

→ Firewall blocking traffic

Solution: Allow VPN traffic to ports 8443, 8080
```

---

## Advanced Topics

### Backup & Restore

**Create Backup:**
1. Management App → **System** → **Backup**
2. Choose what to backup:
   ```
   [√] Database
   [√] Content files
   [√] Configuration
   [√] Certificates (encrypted)
   
   Backup Location: D:\Backups\
   ```
3. Click **Create Backup**

**Restore Backup:**
1. Management App → **System** → **Restore**
2. Select backup file
3. Choose components to restore
4. Confirm (will restart services)

### High Availability

For mission-critical deployments:

1. **Database Replication**
   - SQL Server Always On Availability Groups
   - Or manual failover with backup/restore

2. **Load Balancing**
   - Deploy multiple server instances
   - Use load balancer for WSS connections
   - Shared database backend

3. **Redundant Clients**
   - Deploy backup Pi for critical locations
   - Automatic failover on primary failure

### Integration Examples

**Example 1: Room Booking System**

```sql
-- Data Query for room status
SELECT 
    RoomName,
    CurrentBooking,
    NextBooking,
    AvailableUntil
FROM dbo.vw_RoomStatus
WHERE RoomID = @RoomID
```

Overlay on content shows:
```
Conference Room A
● In Use
Current: Team Standup (10:00-10:30)
Next: Sales Meeting (11:00-12:00)
Available in: 15 minutes
```

**Example 2: Emergency Broadcast**

```csharp
// API call to trigger emergency message
POST /api/v1/emergency/broadcast
{
    "message": "Fire alarm activated. Please evacuate.",
    "priority": "CRITICAL",
    "clients": "ALL",
    "duration": 300
}
```

All displays immediately show emergency message, overriding scheduled content.

---

## Support & Resources

### Documentation
- Architecture: [docs/ARCHITECTURE.md](./ARCHITECTURE.md)
- Security: [docs/SECURITY.md](./SECURITY.md)
- Database: [docs/DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md)
- API Reference: [docs/API_REFERENCE.md](./API_REFERENCE.md)

### Help Resources
- Knowledge Base: [https://makerscreen.local/kb](https://makerscreen.local/kb)
- Video Tutorials: [https://makerscreen.local/videos](https://makerscreen.local/videos)
- Community Forum: [https://makerscreen.local/forum](https://makerscreen.local/forum)

### Support Contacts
- Email: support@makerscreen.local
- Phone: +49 XXX XXXXXXX
- Emergency: +49 XXX XXXXXXX (24/7)

### Training
- Administrator Training: 2-day course
- User Training: 1-day course
- Self-paced online learning available

---

**Deployment Guide Version**: 1.0  
**Last Updated**: 2024-01-15  
**For MakerScreen Version**: 1.0.0
