#!/usr/bin/env python3
"""
MakerScreen Digital Signage Client for Raspberry Pi
Connects to the MakerScreen server via WebSocket and displays content
"""

import asyncio
import websockets
import json
import platform
import uuid
import os
import base64
from datetime import datetime
from pathlib import Path
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('MakerScreenClient')

# Configuration
CONFIG_FILE = '/opt/makerscreen/config.json'
DEFAULT_SERVER_URL = 'ws://localhost:8443'
CONTENT_DIR = '/opt/makerscreen/content'

class MakerScreenClient:
    """Main client application for digital signage display"""
    
    def __init__(self):
        self.websocket = None
        self.running = False
        self.config = self.load_config()
        self.server_url = self.config.get('serverUrl', DEFAULT_SERVER_URL)
        self.client_id = self.get_client_id()
        self.client_name = platform.node()
        
        # Ensure content directory exists
        Path(CONTENT_DIR).mkdir(parents=True, exist_ok=True)
        
    def load_config(self):
        """Load configuration from file"""
        try:
            if os.path.exists(CONFIG_FILE):
                with open(CONFIG_FILE, 'r') as f:
                    return json.load(f)
        except Exception as e:
            logger.warning(f'Could not load config file: {e}')
        
        return {}
    
    def get_client_id(self):
        """Get unique client ID based on MAC address"""
        mac = uuid.getnode()
        return f'{mac:012x}'
    
    async def connect(self):
        """Connect to the WebSocket server"""
        logger.info(f'Connecting to {self.server_url}...')
        try:
            self.websocket = await websockets.connect(self.server_url)
            await self.register()
            logger.info('Connected successfully!')
            return True
        except Exception as e:
            logger.error(f'Connection failed: {e}')
            return False
    
    async def register(self):
        """Register this client with the server"""
        registration = {
            'type': 'REGISTER',
            'clientId': self.client_id,
            'data': {
                'name': self.client_name,
                'macAddress': self.client_id,
                'version': '1.0.0',
                'platform': platform.system(),
                'platformVersion': platform.release()
            },
            'timestamp': datetime.utcnow().isoformat()
        }
        
        await self.websocket.send(json.dumps(registration))
        response = await self.websocket.recv()
        response_data = json.loads(response)
        logger.info(f'Registration response: {response_data}')
    
    async def send_heartbeat(self):
        """Send periodic heartbeat to server"""
        while self.running:
            try:
                heartbeat = {
                    'type': 'HEARTBEAT',
                    'clientId': self.client_id,
                    'timestamp': datetime.utcnow().isoformat()
                }
                await self.websocket.send(json.dumps(heartbeat))
                logger.debug('Heartbeat sent')
                await asyncio.sleep(30)  # Send heartbeat every 30 seconds
            except Exception as e:
                logger.error(f'Heartbeat error: {e}')
                break
    
    async def receive_messages(self):
        """Receive and process messages from server"""
        while self.running:
            try:
                message = await self.websocket.recv()
                data = json.loads(message)
                await self.handle_message(data)
            except websockets.exceptions.ConnectionClosed:
                logger.warning('Connection closed by server')
                break
            except Exception as e:
                logger.error(f'Receive error: {e}')
                break
    
    async def handle_message(self, message):
        """Handle incoming messages from server"""
        msg_type = message.get('type')
        logger.info(f'Received message: {msg_type}')
        
        if msg_type == 'CONTENT_UPDATE':
            await self.handle_content_update(message)
        elif msg_type == 'COMMAND':
            await self.handle_command(message)
        elif msg_type == 'REGISTER':
            logger.info('Registration confirmed')
        else:
            logger.warning(f'Unknown message type: {msg_type}')
    
    async def handle_content_update(self, message):
        """Handle content update from server"""
        try:
            data = message.get('data', {})
            content_id = data.get('contentId')
            content_name = data.get('name')
            content_type = data.get('type')
            mime_type = data.get('mimeType')
            content_data = data.get('data')
            
            logger.info(f'Receiving content: {content_name} ({content_type})')
            
            # Decode base64 content
            if content_data:
                decoded_data = base64.b64decode(content_data)
                
                # Save to content directory
                file_extension = self.get_file_extension(mime_type)
                file_path = os.path.join(CONTENT_DIR, f'{content_id}{file_extension}')
                
                with open(file_path, 'wb') as f:
                    f.write(decoded_data)
                
                logger.info(f'Content saved to {file_path}')
                
                # Display the content
                await self.display_content(file_path, content_type)
            
            # Send acknowledgment
            ack = {
                'type': 'STATUS',
                'clientId': self.client_id,
                'data': {
                    'status': 'content_received',
                    'contentId': content_id
                },
                'timestamp': datetime.utcnow().isoformat()
            }
            await self.websocket.send(json.dumps(ack))
            
        except Exception as e:
            logger.error(f'Error handling content update: {e}')
    
    async def handle_command(self, message):
        """Handle command from server"""
        try:
            data = message.get('data', {})
            command = data.get('command')
            
            logger.info(f'Executing command: {command}')
            
            if command == 'reboot':
                logger.warning('Reboot command received')
                # os.system('sudo reboot')
            elif command == 'update':
                logger.info('Update command received')
                # Perform client update
            elif command == 'clear_content':
                logger.info('Clearing content')
                self.clear_content()
            
        except Exception as e:
            logger.error(f'Error handling command: {e}')
    
    async def display_content(self, file_path, content_type):
        """Display content on screen"""
        logger.info(f'Displaying {content_type}: {file_path}')
        
        # In a full implementation, this would:
        # 1. Use PyQt5 or Tkinter to display images/videos
        # 2. Use a web browser component for HTML content
        # 3. Handle content rotation and scheduling
        
        # For now, just log that content would be displayed
        logger.info(f'Content would be displayed: {file_path}')
    
    def clear_content(self):
        """Clear all cached content"""
        try:
            for file in os.listdir(CONTENT_DIR):
                file_path = os.path.join(CONTENT_DIR, file)
                if os.path.isfile(file_path):
                    os.remove(file_path)
            logger.info('Content cleared')
        except Exception as e:
            logger.error(f'Error clearing content: {e}')
    
    def get_file_extension(self, mime_type):
        """Get file extension from MIME type"""
        mime_map = {
            'image/png': '.png',
            'image/jpeg': '.jpg',
            'image/gif': '.gif',
            'video/mp4': '.mp4',
            'text/html': '.html'
        }
        return mime_map.get(mime_type, '.bin')
    
    async def run(self):
        """Main run loop with automatic reconnection"""
        self.running = True
        
        logger.info('===========================================')
        logger.info('MakerScreen Client Starting')
        logger.info(f'Client ID: {self.client_id}')
        logger.info(f'Client Name: {self.client_name}')
        logger.info(f'Server URL: {self.server_url}')
        logger.info('===========================================')
        
        while self.running:
            if await self.connect():
                try:
                    # Run heartbeat and message receiver concurrently
                    await asyncio.gather(
                        self.send_heartbeat(),
                        self.receive_messages()
                    )
                except Exception as e:
                    logger.error(f'Error during operation: {e}')
            
            # Reconnect after 5 seconds if connection lost
            if self.running:
                logger.info('Connection lost, reconnecting in 5 seconds...')
                await asyncio.sleep(5)
    
    def stop(self):
        """Stop the client"""
        logger.info('Stopping client...')
        self.running = False

if __name__ == '__main__':
    client = MakerScreenClient()
    try:
        asyncio.run(client.run())
    except KeyboardInterrupt:
        logger.info('Client stopped by user')
        client.stop()
