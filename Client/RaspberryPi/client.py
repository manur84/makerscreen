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
import threading
import signal
import sys
from datetime import datetime
from pathlib import Path
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.StreamHandler(),
        logging.FileHandler('/opt/makerscreen/client.log', mode='a')
    ] if os.path.exists('/opt/makerscreen') else [logging.StreamHandler()]
)
logger = logging.getLogger('MakerScreenClient')

# Configuration
CONFIG_FILE = '/opt/makerscreen/config.json'
DEFAULT_SERVER_URL = 'ws://localhost:8443'
CONTENT_DIR = '/opt/makerscreen/content'
VERSION = '1.0.0'


class ContentCache:
    """Manages local content caching"""
    
    def __init__(self, cache_dir):
        self.cache_dir = Path(cache_dir)
        self.cache_dir.mkdir(parents=True, exist_ok=True)
        self.manifest = {}
        self._load_manifest()
    
    def _load_manifest(self):
        manifest_path = self.cache_dir / 'manifest.json'
        try:
            if manifest_path.exists():
                with open(manifest_path, 'r') as f:
                    self.manifest = json.load(f)
        except Exception as e:
            logger.error(f"Error loading cache manifest: {e}")
    
    def _save_manifest(self):
        manifest_path = self.cache_dir / 'manifest.json'
        try:
            with open(manifest_path, 'w') as f:
                json.dump(self.manifest, f, indent=2)
        except Exception as e:
            logger.error(f"Error saving cache manifest: {e}")
    
    def save_content(self, content_id, data, mime_type):
        """Save content to cache"""
        ext = self._get_extension(mime_type)
        filename = f"{content_id}{ext}"
        filepath = self.cache_dir / filename
        
        try:
            with open(filepath, 'wb') as f:
                f.write(data)
            
            self.manifest[content_id] = {
                'filename': filename,
                'mime_type': mime_type,
                'cached_at': datetime.utcnow().isoformat()
            }
            self._save_manifest()
            
            logger.info(f"Content cached: {filename}")
            return str(filepath)
        except Exception as e:
            logger.error(f"Error caching content: {e}")
            return None
    
    def get_content_path(self, content_id):
        """Get path to cached content"""
        if content_id in self.manifest:
            filename = self.manifest[content_id]['filename']
            filepath = self.cache_dir / filename
            if filepath.exists():
                return str(filepath)
        return None
    
    def has_content(self, content_id):
        """Check if content is cached"""
        return self.get_content_path(content_id) is not None
    
    def clear(self):
        """Clear all cached content"""
        try:
            for filepath in self.cache_dir.iterdir():
                if filepath.is_file():
                    filepath.unlink()
            self.manifest = {}
            self._save_manifest()
            logger.info("Cache cleared")
        except Exception as e:
            logger.error(f"Error clearing cache: {e}")
    
    def _get_extension(self, mime_type):
        mime_map = {
            'image/png': '.png',
            'image/jpeg': '.jpg',
            'image/gif': '.gif',
            'video/mp4': '.mp4',
            'text/html': '.html'
        }
        return mime_map.get(mime_type, '.bin')


class DisplayManager:
    """Manages display integration"""
    
    def __init__(self):
        self.display = None
        self.app = None
        self.display_thread = None
        self._initialized = False
    
    def initialize(self):
        """Initialize the display engine"""
        if self._initialized:
            return
        
        try:
            # Only import PyQt5 if we're going to use it
            if os.environ.get('DISPLAY') or os.path.exists('/dev/fb0'):
                from display_engine import create_display
                self.app, self.display = create_display()
                self._initialized = True
                logger.info("Display engine initialized")
            else:
                logger.warning("No display available, running in headless mode")
        except Exception as e:
            logger.error(f"Could not initialize display: {e}")
    
    def start(self):
        """Start the display in a separate thread"""
        if not self._initialized:
            return
        
        def run_display():
            try:
                self.display.show_fullscreen()
                self.app.exec_()
            except Exception as e:
                logger.error(f"Display error: {e}")
        
        self.display_thread = threading.Thread(target=run_display, daemon=True)
        self.display_thread.start()
    
    def show_content(self, content_data):
        """Display content"""
        if self._initialized and self.display:
            self.display.signals.content_update.emit(content_data)
    
    def show_overlay(self, overlay_data):
        """Show overlay"""
        if self._initialized and self.display:
            self.display.signals.overlay_update.emit(overlay_data)
    
    def show_message(self, message):
        """Show a message on screen"""
        if self._initialized and self.display:
            self.display.signals.show_message.emit(message)


class MakerScreenClient:
    """Main client application for digital signage display"""
    
    def __init__(self):
        self.websocket = None
        self.running = False
        self.config = self.load_config()
        self.server_url = self.config.get('serverUrl', DEFAULT_SERVER_URL)
        self.client_id = self.get_client_id()
        self.client_name = self.config.get('displayName') or platform.node()
        self.content_cache = ContentCache(CONTENT_DIR)
        self.display_manager = DisplayManager()
        self.current_playlist = None
        self.playlist_index = 0
        self.connected = False
        
        # Initialize display if available
        self.display_manager.initialize()
        
    def load_config(self):
        """Load configuration from file"""
        try:
            if os.path.exists(CONFIG_FILE):
                with open(CONFIG_FILE, 'r') as f:
                    return json.load(f)
        except Exception as e:
            logger.warning(f'Could not load config file: {e}')
        
        return {'serverUrl': DEFAULT_SERVER_URL, 'autoStart': True}
    
    def save_config(self, config):
        """Save configuration to file"""
        try:
            os.makedirs(os.path.dirname(CONFIG_FILE), exist_ok=True)
            with open(CONFIG_FILE, 'w') as f:
                json.dump(config, f, indent=2)
            self.config = config
            return True
        except Exception as e:
            logger.error(f"Error saving config: {e}")
            return False
    
    def get_client_id(self):
        """Get unique client ID based on MAC address"""
        mac = uuid.getnode()
        return f'{mac:012x}'
    
    async def connect(self):
        """Connect to the WebSocket server"""
        logger.info(f'Connecting to {self.server_url}...')
        self.display_manager.show_message(f"Connecting to\n{self.server_url}")
        
        try:
            self.websocket = await websockets.connect(
                self.server_url,
                ping_interval=20,
                ping_timeout=30,
                close_timeout=10
            )
            await self.register()
            self.connected = True
            logger.info('Connected successfully!')
            self.display_manager.show_message("Connected!\nWaiting for content...")
            return True
        except Exception as e:
            logger.error(f'Connection failed: {e}')
            self.connected = False
            self.display_manager.show_message(f"Connection failed\n{str(e)[:50]}")
            return False
    
    async def register(self):
        """Register this client with the server"""
        import psutil
        
        registration = {
            'type': 'REGISTER',
            'clientId': self.client_id,
            'data': {
                'name': self.client_name,
                'macAddress': self.client_id,
                'version': VERSION,
                'platform': platform.system(),
                'platformVersion': platform.release(),
                'machine': platform.machine(),
                'cpuCount': os.cpu_count(),
                'memoryMb': psutil.virtual_memory().total // (1024 * 1024) if 'psutil' in sys.modules else 0
            },
            'timestamp': datetime.utcnow().isoformat()
        }
        
        await self.websocket.send(json.dumps(registration))
        response = await self.websocket.recv()
        response_data = json.loads(response)
        logger.info(f'Registration response: {response_data}')
    
    async def send_heartbeat(self):
        """Send periodic heartbeat to server"""
        while self.running and self.connected:
            try:
                heartbeat = {
                    'type': 'HEARTBEAT',
                    'clientId': self.client_id,
                    'data': {
                        'status': 'online',
                        'uptime': int(datetime.utcnow().timestamp())
                    },
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
        while self.running and self.connected:
            try:
                message = await self.websocket.recv()
                data = json.loads(message)
                await self.handle_message(data)
            except websockets.exceptions.ConnectionClosed:
                logger.warning('Connection closed by server')
                self.connected = False
                break
            except Exception as e:
                logger.error(f'Receive error: {e}')
                break
    
    async def handle_message(self, message):
        """Handle incoming messages from server"""
        msg_type = message.get('type')
        logger.info(f'Received message: {msg_type}')
        
        handlers = {
            'CONTENT_UPDATE': self.handle_content_update,
            'COMMAND': self.handle_command,
            'REGISTER': lambda m: logger.info('Registration confirmed'),
            'PLAYLIST_UPDATE': self.handle_playlist_update,
            'OVERLAY_UPDATE': self.handle_overlay_update
        }
        
        handler = handlers.get(msg_type)
        if handler:
            await handler(message)
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
                
                # Save to cache
                file_path = self.content_cache.save_content(content_id, decoded_data, mime_type)
                
                if file_path:
                    logger.info(f'Content saved to {file_path}')
                    
                    # Display the content
                    self.display_manager.show_content({
                        'type': content_type,
                        'path': file_path,
                        'data': content_data
                    })
            
            # Send acknowledgment
            await self.send_status('content_received', {'contentId': content_id})
            
        except Exception as e:
            logger.error(f'Error handling content update: {e}')
    
    async def handle_playlist_update(self, message):
        """Handle playlist update from server"""
        try:
            data = message.get('data', {})
            self.current_playlist = data.get('playlist', {})
            self.playlist_index = 0
            
            logger.info(f"Playlist updated: {len(self.current_playlist.get('items', []))} items")
            
            # Start playlist playback
            asyncio.create_task(self.play_playlist())
            
        except Exception as e:
            logger.error(f"Error handling playlist update: {e}")
    
    async def handle_overlay_update(self, message):
        """Handle overlay update from server"""
        try:
            data = message.get('data', {})
            self.display_manager.show_overlay(data)
        except Exception as e:
            logger.error(f"Error handling overlay update: {e}")
    
    async def handle_command(self, message):
        """Handle command from server"""
        try:
            data = message.get('data', {})
            command = data.get('command')
            params = data.get('parameters', {})
            
            logger.info(f'Executing command: {command}')
            
            commands = {
                'reboot': self._cmd_reboot,
                'update': self._cmd_update,
                'clear_content': self._cmd_clear_content,
                'refresh_config': self._cmd_refresh_config,
                'screenshot': self._cmd_screenshot,
                'show_message': lambda p: self.display_manager.show_message(p.get('message', ''))
            }
            
            cmd_handler = commands.get(command)
            if cmd_handler:
                await self._execute_command(cmd_handler, params)
            else:
                logger.warning(f"Unknown command: {command}")
            
        except Exception as e:
            logger.error(f'Error handling command: {e}')
    
    async def _execute_command(self, handler, params):
        """Execute a command handler"""
        if asyncio.iscoroutinefunction(handler):
            await handler(params)
        else:
            handler(params)
    
    def _cmd_reboot(self, params):
        logger.warning('Reboot command received')
        # os.system('sudo reboot')
    
    def _cmd_update(self, params):
        logger.info('Update command received')
        # Perform client update
    
    def _cmd_clear_content(self, params):
        logger.info('Clearing content')
        self.content_cache.clear()
    
    def _cmd_refresh_config(self, params):
        logger.info('Refreshing configuration')
        self.config = self.load_config()
        self.server_url = self.config.get('serverUrl', DEFAULT_SERVER_URL)
    
    def _cmd_screenshot(self, params):
        logger.info('Screenshot requested')
        # Capture and send screenshot
    
    async def play_playlist(self):
        """Play through the current playlist"""
        if not self.current_playlist:
            return
        
        items = self.current_playlist.get('items', [])
        while self.running and items:
            item = items[self.playlist_index]
            content_id = item.get('contentId')
            duration = item.get('duration', 10)
            
            # Get content from cache or request it
            content_path = self.content_cache.get_content_path(content_id)
            if content_path:
                self.display_manager.show_content({
                    'type': 'image',
                    'path': content_path
                })
            
            await asyncio.sleep(duration)
            self.playlist_index = (self.playlist_index + 1) % len(items)
    
    async def send_status(self, status, data=None):
        """Send status update to server"""
        try:
            message = {
                'type': 'STATUS',
                'clientId': self.client_id,
                'data': {
                    'status': status,
                    **(data or {})
                },
                'timestamp': datetime.utcnow().isoformat()
            }
            await self.websocket.send(json.dumps(message))
        except Exception as e:
            logger.error(f"Error sending status: {e}")
    
    async def run(self):
        """Main run loop with automatic reconnection"""
        self.running = True
        
        logger.info('===========================================')
        logger.info('MakerScreen Client Starting')
        logger.info(f'Client ID: {self.client_id}')
        logger.info(f'Client Name: {self.client_name}')
        logger.info(f'Server URL: {self.server_url}')
        logger.info(f'Version: {VERSION}')
        logger.info('===========================================')
        
        # Start display
        self.display_manager.start()
        
        reconnect_delay = 5
        max_reconnect_delay = 60
        
        while self.running:
            if await self.connect():
                reconnect_delay = 5  # Reset delay on successful connection
                try:
                    # Run heartbeat and message receiver concurrently
                    await asyncio.gather(
                        self.send_heartbeat(),
                        self.receive_messages()
                    )
                except Exception as e:
                    logger.error(f'Error during operation: {e}')
            
            self.connected = False
            
            # Reconnect with exponential backoff
            if self.running:
                logger.info(f'Connection lost, reconnecting in {reconnect_delay} seconds...')
                self.display_manager.show_message(f"Reconnecting in\n{reconnect_delay} seconds...")
                await asyncio.sleep(reconnect_delay)
                reconnect_delay = min(reconnect_delay * 2, max_reconnect_delay)
    
    def stop(self):
        """Stop the client"""
        logger.info('Stopping client...')
        self.running = False


def run_web_ui(client):
    """Run the web UI in a separate thread"""
    try:
        from web_ui import run_webui
        threading.Thread(target=run_webui, args=(5001,), daemon=True).start()
        logger.info("Web UI started on port 5001")
    except Exception as e:
        logger.warning(f"Could not start web UI: {e}")


def main():
    client = MakerScreenClient()
    
    # Setup signal handlers
    def signal_handler(sig, frame):
        logger.info('Shutdown signal received')
        client.stop()
        sys.exit(0)
    
    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)
    
    # Start web UI
    run_web_ui(client)
    
    # Run main client loop
    try:
        asyncio.run(client.run())
    except KeyboardInterrupt:
        logger.info('Client stopped by user')
        client.stop()


if __name__ == '__main__':
    main()
