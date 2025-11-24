using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MakerScreen.Services.Deployment;

/// <summary>
/// Handles automatic deployment and installation of clients
/// </summary>
public class ClientDeploymentService : IClientDeploymentService
{
    private readonly ILogger<ClientDeploymentService> _logger;
    private readonly IWebSocketServer _webSocketServer;
    private readonly string _deploymentPath;
    private const string ClientVersion = "1.0.0";

    public ClientDeploymentService(
        ILogger<ClientDeploymentService> logger,
        IWebSocketServer webSocketServer)
    {
        _logger = logger;
        _webSocketServer = webSocketServer;
        _deploymentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Deployments");
        Directory.CreateDirectory(_deploymentPath);
    }

    public async Task<DeploymentPackage> CreateDeploymentPackageAsync(
        Dictionary<string, string> configuration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating deployment package");

        try
        {
            // Create the Python client files
            var clientFiles = GenerateClientFiles(configuration);
            
            // Package everything into a zip
            var packageData = await CreateZipPackageAsync(clientFiles, cancellationToken);
            
            // Calculate hash
            var hash = CalculateHash(packageData);
            
            var package = new DeploymentPackage
            {
                Version = ClientVersion,
                PackageData = packageData,
                Hash = hash,
                Configuration = configuration,
                CreatedAt = DateTime.UtcNow
            };
            
            // Save package to disk
            var packagePath = Path.Combine(_deploymentPath, $"client_{ClientVersion}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
            await File.WriteAllBytesAsync(packagePath, packageData, cancellationToken);
            
            _logger.LogInformation("Deployment package created: {PackagePath}", packagePath);
            
            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deployment package");
            throw;
        }
    }

    public async Task<bool> DeployToClientAsync(
        string clientIp,
        DeploymentPackage package,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deploying to client at {ClientIp}", clientIp);

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            
            // SSH into Raspberry Pi and deploy
            // This would use SSH.NET or similar library in production
            var deploymentScript = GenerateDeploymentScript(package);
            
            // For now, we'll create a deployment endpoint that the client can call
            // In production, this would use SSH or a dedicated deployment protocol
            
            var deployUrl = $"http://{clientIp}:5000/deploy";
            var content = new ByteArrayContent(package.PackageData);
            content.Headers.Add("X-Package-Hash", package.Hash);
            content.Headers.Add("X-Package-Version", package.Version);
            
            var response = await httpClient.PostAsync(deployUrl, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deployed to {ClientIp}", clientIp);
                return true;
            }
            else
            {
                _logger.LogWarning("Deployment to {ClientIp} failed with status {StatusCode}", clientIp, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying to client {ClientIp}", clientIp);
            return false;
        }
    }

    public async Task<bool> AutoDiscoverAndDeployAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Auto-discovering clients on network");

        try
        {
            // Scan local network for Raspberry Pi devices
            var discoveredClients = await DiscoverRaspberryPiDevicesAsync(cancellationToken);
            
            _logger.LogInformation("Discovered {Count} potential clients", discoveredClients.Count);
            
            // Create deployment package
            var config = new Dictionary<string, string>
            {
                ["serverUrl"] = GetServerUrl(),
                ["autoStart"] = "true"
            };
            
            var package = await CreateDeploymentPackageAsync(config, cancellationToken);
            
            // Deploy to each discovered client
            var deploymentTasks = discoveredClients.Select(ip => DeployToClientAsync(ip, package, cancellationToken));
            var results = await Task.WhenAll(deploymentTasks);
            
            var successCount = results.Count(r => r);
            _logger.LogInformation("Successfully deployed to {SuccessCount}/{TotalCount} clients", successCount, discoveredClients.Count);
            
            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-discovery and deployment");
            return false;
        }
    }

    public async Task<string> GenerateRaspberryPiImageAsync(
        DeploymentPackage package,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Raspberry Pi image");

        try
        {
            var imagePath = Path.Combine(_deploymentPath, $"makerscreen_{package.Version}.img");
            
            // This would create a custom Raspberry Pi OS image with:
            // - Pre-installed Python client
            // - Auto-start configuration
            // - Network configuration
            // - SSH enabled
            
            // For demonstration, we'll create a script-based installer
            var installerScript = GenerateInstallerScript(package);
            var scriptPath = Path.Combine(_deploymentPath, "install.sh");
            await File.WriteAllTextAsync(scriptPath, installerScript, cancellationToken);
            
            _logger.LogInformation("Installer script created at {ScriptPath}", scriptPath);
            
            return scriptPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Raspberry Pi image");
            throw;
        }
    }

    private Dictionary<string, string> GenerateClientFiles(Dictionary<string, string> configuration)
    {
        var files = new Dictionary<string, string>();
        
        // Main client application
        files["client.py"] = GeneratePythonClient(configuration);
        
        // Requirements file
        files["requirements.txt"] = @"websockets==12.0
pillow==10.1.0
requests==2.31.0
";
        
        // Systemd service file
        files["makerscreen.service"] = GenerateSystemdService();
        
        // Installation script
        files["install.sh"] = GenerateInstallScript(configuration);
        
        // Configuration file
        files["config.json"] = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
        
        return files;
    }

    private string GeneratePythonClient(Dictionary<string, string> configuration)
    {
        var serverUrl = configuration.GetValueOrDefault("serverUrl", "ws://localhost:8443");
        
        return $@"#!/usr/bin/env python3
import asyncio
import websockets
import json
import platform
import uuid
import os
from datetime import datetime

# Configuration
SERVER_URL = '{serverUrl}'
CLIENT_ID = str(uuid.getnode())  # MAC address as ID
CLIENT_NAME = platform.node()

class MakerScreenClient:
    def __init__(self):
        self.websocket = None
        self.running = False
        
    async def connect(self):
        print(f'Connecting to {{SERVER_URL}}...')
        try:
            self.websocket = await websockets.connect(SERVER_URL)
            await self.register()
            print('Connected successfully!')
            return True
        except Exception as e:
            print(f'Connection failed: {{e}}')
            return False
    
    async def register(self):
        registration = {{
            'type': 'REGISTER',
            'clientId': CLIENT_ID,
            'data': {{
                'name': CLIENT_NAME,
                'macAddress': CLIENT_ID,
                'version': '1.0.0',
                'platform': platform.system()
            }}
        }}
        await self.websocket.send(json.dumps(registration))
        response = await self.websocket.recv()
        print(f'Registration response: {{response}}')
    
    async def send_heartbeat(self):
        while self.running:
            try:
                heartbeat = {{
                    'type': 'HEARTBEAT',
                    'clientId': CLIENT_ID,
                    'timestamp': datetime.utcnow().isoformat()
                }}
                await self.websocket.send(json.dumps(heartbeat))
                await asyncio.sleep(30)  # Send heartbeat every 30 seconds
            except Exception as e:
                print(f'Heartbeat error: {{e}}')
                break
    
    async def receive_messages(self):
        while self.running:
            try:
                message = await self.websocket.recv()
                data = json.loads(message)
                await self.handle_message(data)
            except Exception as e:
                print(f'Receive error: {{e}}')
                break
    
    async def handle_message(self, message):
        msg_type = message.get('type')
        print(f'Received message: {{msg_type}}')
        
        if msg_type == 'CONTENT_UPDATE':
            await self.handle_content_update(message)
        elif msg_type == 'COMMAND':
            await self.handle_command(message)
    
    async def handle_content_update(self, message):
        print('Content update received')
        # Handle content updates here
    
    async def handle_command(self, message):
        print(f'Command received: {{message}}')
        # Handle commands here
    
    async def run(self):
        self.running = True
        
        while self.running:
            if await self.connect():
                await asyncio.gather(
                    self.send_heartbeat(),
                    self.receive_messages()
                )
            
            # Reconnect after 5 seconds if connection lost
            print('Connection lost, reconnecting in 5 seconds...')
            await asyncio.sleep(5)

if __name__ == '__main__':
    client = MakerScreenClient()
    try:
        asyncio.run(client.run())
    except KeyboardInterrupt:
        print('Client stopped')
";
    }

    private string GenerateSystemdService()
    {
        return @"[Unit]
Description=MakerScreen Digital Signage Client
After=network.target

[Service]
Type=simple
User=pi
WorkingDirectory=/opt/makerscreen
ExecStart=/usr/bin/python3 /opt/makerscreen/client.py
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
";
    }

    private string GenerateInstallScript(Dictionary<string, string> configuration)
    {
        return @"#!/bin/bash
set -e

echo 'Installing MakerScreen Client...'

# Create installation directory
sudo mkdir -p /opt/makerscreen
sudo chown -R $USER:$USER /opt/makerscreen

# Copy files
cp client.py /opt/makerscreen/
cp config.json /opt/makerscreen/
cp requirements.txt /opt/makerscreen/

# Install Python dependencies
cd /opt/makerscreen
pip3 install -r requirements.txt

# Install systemd service
sudo cp makerscreen.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable makerscreen
sudo systemctl start makerscreen

echo 'MakerScreen Client installed successfully!'
echo 'Service status:'
sudo systemctl status makerscreen
";
    }

    private string GenerateDeploymentScript(DeploymentPackage package)
    {
        return $@"#!/bin/bash
# Automatic deployment script
# Package version: {package.Version}
# Package hash: {package.Hash}

# Extract and install
unzip -o package.zip -d /tmp/makerscreen_deploy
cd /tmp/makerscreen_deploy
chmod +x install.sh
sudo ./install.sh
";
    }

    private string GenerateInstallerScript(DeploymentPackage package)
    {
        return @"#!/bin/bash
# MakerScreen Zero-Touch Installer
# This script will be embedded in the Raspberry Pi image

set -e

echo '========================================='
echo 'MakerScreen Client Auto-Installer'
echo '========================================='

# Update system
echo 'Updating system packages...'
sudo apt-get update
sudo apt-get upgrade -y

# Install dependencies
echo 'Installing dependencies...'
sudo apt-get install -y python3 python3-pip git

# Download and install client
echo 'Installing MakerScreen client...'
cd /tmp
# In production, this would download from the server
# For now, it uses the embedded package

chmod +x install.sh
./install.sh

echo '========================================='
echo 'Installation complete!'
echo 'MakerScreen client is now running.'
echo '========================================='
";
    }

    private async Task<byte[]> CreateZipPackageAsync(Dictionary<string, string> files, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Key);
                using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(file.Value);
                await entryStream.WriteAsync(bytes, cancellationToken);
            }
        }
        
        return memoryStream.ToArray();
    }

    private string CalculateHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToHexString(hashBytes);
    }

    private async Task<List<string>> DiscoverRaspberryPiDevicesAsync(CancellationToken cancellationToken)
    {
        // In production, this would:
        // 1. Scan local network using ARP or mDNS
        // 2. Identify Raspberry Pi devices by MAC address prefix
        // 3. Return list of IP addresses
        
        // For demonstration, return empty list
        await Task.Delay(100, cancellationToken);
        return new List<string>();
    }

    private string GetServerUrl()
    {
        // Get local IP address
        var hostName = System.Net.Dns.GetHostName();
        var addresses = System.Net.Dns.GetHostAddresses(hostName);
        var localIp = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        
        return $"ws://{localIp}:8443";
    }
}
