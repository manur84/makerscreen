#!/usr/bin/env python3
"""
MakerScreen Client - Raspberry Pi Digital Signage Client
"""

import asyncio
import json
import logging
import socket
import sys
from datetime import datetime
from pathlib import Path

import websockets
from PyQt5.QtWidgets import QApplication, QLabel, QMainWindow
from PyQt5.QtCore import Qt, QTimer
from PyQt5.QtGui import QPixmap

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('/var/log/makerscreen/client.log'),
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger(__name__)


class MakerScreenClient:
    def __init__(self):
        self.hostname = socket.gethostname()
        self.mac_address = self.get_mac_address()
        self.ip_address = self.get_ip_address()
        self.server_url = None
        self.ws = None
        self.running = True

    def get_mac_address(self):
        """Get MAC address"""
        try:
            with open('/sys/class/net/eth0/address', 'r') as f:
                return f.read().strip()
        except:
            return "00:00:00:00:00:00"

    def get_ip_address(self):
        """Get IP address"""
        try:
            s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            s.connect(("8.8.8.8", 80))
            ip = s.getsockname()[0]
            s.close()
            return ip
        except:
            return "0.0.0.0"

    async def discover_server(self):
        """Discover server via mDNS"""
        # TODO: Implement mDNS discovery
        # For now, use default
        self.server_url = "ws://localhost:8080/ws/"
        logger.info(f"Server URL: {self.server_url}")

    async def connect(self):
        """Connect to WebSocket server"""
        try:
            logger.info(f"Connecting to {self.server_url}")
            self.ws = await websockets.connect(self.server_url)
            logger.info("Connected to server")
            
            # Register with server
            await self.register()
            
            # Start heartbeat
            asyncio.create_task(self.heartbeat_loop())
            
            # Listen for messages
            await self.listen()
            
        except Exception as e:
            logger.error(f"Connection error: {e}")
            await asyncio.sleep(5)
            await self.connect()

    async def register(self):
        """Register client with server"""
        message = {
            "type": "register",
            "data": {
                "hostname": self.hostname,
                "mac_address": self.mac_address,
                "ip_address": self.ip_address,
                "model": "Raspberry Pi",
                "os_version": "Raspberry Pi OS"
            }
        }
        await self.ws.send(json.dumps(message))
        logger.info("Registration sent")

    async def heartbeat_loop(self):
        """Send periodic heartbeat"""
        while self.running and self.ws:
            try:
                message = {
                    "type": "heartbeat",
                    "data": {
                        "timestamp": datetime.utcnow().isoformat()
                    }
                }
                await self.ws.send(json.dumps(message))
                await asyncio.sleep(30)
            except Exception as e:
                logger.error(f"Heartbeat error: {e}")
                break

    async def listen(self):
        """Listen for messages from server"""
        try:
            async for message in self.ws:
                data = json.loads(message)
                await self.handle_message(data)
        except Exception as e:
            logger.error(f"Listen error: {e}")

    async def handle_message(self, data):
        """Handle incoming message"""
        msg_type = data.get('type')
        logger.info(f"Received message: {msg_type}")
        
        if msg_type == 'registered':
            logger.info("Successfully registered with server")
        elif msg_type == 'content_update':
            logger.info("Content update received")
        elif msg_type == 'command':
            await self.handle_command(data.get('data'))

    async def handle_command(self, command):
        """Handle server command"""
        cmd = command.get('command')
        logger.info(f"Executing command: {cmd}")
        # TODO: Implement commands


class DisplayWindow(QMainWindow):
    """Main display window"""
    
    def __init__(self):
        super().__init__()
        self.init_ui()
        
    def init_ui(self):
        """Initialize UI"""
        self.setWindowTitle('MakerScreen Display')
        self.showFullScreen()
        
        # Create label for content
        self.label = QLabel(self)
        self.label.setAlignment(Qt.AlignCenter)
        self.label.setStyleSheet("background-color: black; color: white;")
        self.setCentralWidget(self.label)
        
        # Show status screen by default
        self.show_status_screen()
        
    def show_status_screen(self):
        """Show status screen when no content"""
        status_text = f"""
        <div style='text-align: center; padding: 50px;'>
            <h1>MakerScreen Client</h1>
            <p style='font-size: 24px;'>Waiting for connection...</p>
            <p>Hostname: {socket.gethostname()}</p>
            <p>Status: Connecting</p>
            <div style='margin-top: 50px;'>
                <p>Scan QR code to configure:</p>
                <p style='font-size: 48px;'>⬛⬜⬛⬜⬛</p>
                <p>http://{self.get_ip()}:8080</p>
            </div>
        </div>
        """
        self.label.setText(status_text)
        
    def get_ip(self):
        """Get IP address"""
        try:
            s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            s.connect(("8.8.8.8", 80))
            ip = s.getsockname()[0]
            s.close()
            return ip
        except:
            return "0.0.0.0"

    def display_content(self, image_path):
        """Display content image"""
        pixmap = QPixmap(image_path)
        self.label.setPixmap(pixmap.scaled(
            self.size(), 
            Qt.KeepAspectRatio, 
            Qt.SmoothTransformation
        ))


def main():
    """Main entry point"""
    logger.info("Starting MakerScreen Client")
    
    # Start Qt application
    app = QApplication(sys.argv)
    window = DisplayWindow()
    window.show()
    
    # Start WebSocket client in background
    client = MakerScreenClient()
    
    async def run_client():
        await client.discover_server()
        await client.connect()
    
    # Run asyncio event loop
    import qasync
    loop = qasync.QEventLoop(app)
    asyncio.set_event_loop(loop)
    
    with loop:
        loop.create_task(run_client())
        loop.run_forever()


if __name__ == '__main__':
    main()
