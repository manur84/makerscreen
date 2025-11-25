#!/usr/bin/env python3
"""
MakerScreen Display Engine using PyQt5
Displays content on the screen with support for images, videos, and overlays
"""

import sys
import os
from PyQt5.QtWidgets import (
    QApplication, QMainWindow, QLabel, QWidget, 
    QVBoxLayout, QStackedWidget, QGraphicsOpacityEffect
)
from PyQt5.QtCore import Qt, QTimer, QPropertyAnimation, pyqtSignal, QObject
from PyQt5.QtGui import QPixmap, QFont, QColor, QPalette, QImage
from PIL import Image
import io
import logging

logger = logging.getLogger('DisplayEngine')


class SignalBridge(QObject):
    """Bridge for thread-safe signals"""
    content_update = pyqtSignal(dict)
    overlay_update = pyqtSignal(dict)
    show_message = pyqtSignal(str)


class OverlayWidget(QLabel):
    """Widget for displaying overlays"""
    
    def __init__(self, overlay_config):
        super().__init__()
        self.config = overlay_config
        self.setup_style()
    
    def setup_style(self):
        style = self.config.get('style', {})
        position = self.config.get('position', {})
        
        font_family = style.get('fontFamily', 'Arial')
        font_size = style.get('fontSize', 24)
        font_color = style.get('fontColor', '#FFFFFF')
        bg_color = style.get('backgroundColor', '#00000080')
        border_radius = style.get('borderRadius', 5)
        padding = style.get('padding', 10)
        
        self.setFont(QFont(font_family, font_size))
        self.setStyleSheet(f"""
            QLabel {{
                color: {font_color};
                background-color: {bg_color};
                border-radius: {border_radius}px;
                padding: {padding}px;
            }}
        """)
        
        self.move(position.get('x', 0), position.get('y', 0))
        self.resize(position.get('width', 200), position.get('height', 50))
    
    def update_content(self, content):
        self.setText(content)


class ContentDisplay(QLabel):
    """Widget for displaying main content (images/videos)"""
    
    def __init__(self):
        super().__init__()
        self.setAlignment(Qt.AlignCenter)
        self.setScaledContents(True)
        self.setStyleSheet("background-color: black;")
    
    def show_image(self, image_data):
        """Display image from bytes"""
        try:
            image = QImage()
            if isinstance(image_data, bytes):
                image.loadFromData(image_data)
            elif isinstance(image_data, str) and os.path.exists(image_data):
                image.load(image_data)
            
            if not image.isNull():
                pixmap = QPixmap.fromImage(image)
                scaled = pixmap.scaled(
                    self.size(),
                    Qt.KeepAspectRatio,
                    Qt.SmoothTransformation
                )
                self.setPixmap(scaled)
                logger.info("Image displayed successfully")
        except Exception as e:
            logger.error(f"Error displaying image: {e}")
    
    def show_message(self, message):
        """Display text message"""
        self.clear()
        self.setText(message)
        self.setStyleSheet("""
            QLabel {
                background-color: black;
                color: white;
                font-size: 32px;
                font-family: Arial;
            }
        """)


class PlaylistManager:
    """Manages playlist playback"""
    
    def __init__(self, display_callback):
        self.playlist = []
        self.current_index = 0
        self.timer = QTimer()
        self.timer.timeout.connect(self._next_item)
        self.display_callback = display_callback
    
    def set_playlist(self, playlist_data):
        """Set playlist items"""
        self.playlist = playlist_data.get('items', [])
        self.current_index = 0
        logger.info(f"Playlist set with {len(self.playlist)} items")
    
    def start(self):
        """Start playlist playback"""
        if self.playlist:
            self._show_current()
    
    def stop(self):
        """Stop playlist playback"""
        self.timer.stop()
    
    def _show_current(self):
        """Show current playlist item"""
        if not self.playlist:
            return
        
        item = self.playlist[self.current_index]
        duration = item.get('duration', 10) * 1000  # Convert to milliseconds
        
        self.display_callback(item)
        self.timer.start(duration)
    
    def _next_item(self):
        """Move to next playlist item"""
        self.current_index = (self.current_index + 1) % len(self.playlist)
        self._show_current()


class MainWindow(QMainWindow):
    """Main display window"""
    
    def __init__(self):
        super().__init__()
        self.signals = SignalBridge()
        self.overlays = {}
        self.setup_ui()
        self.connect_signals()
    
    def setup_ui(self):
        """Setup the main UI"""
        self.setWindowTitle("MakerScreen Display")
        self.setStyleSheet("background-color: black;")
        
        # Central widget
        central = QWidget()
        self.setCentralWidget(central)
        
        layout = QVBoxLayout(central)
        layout.setContentsMargins(0, 0, 0, 0)
        
        # Content display
        self.content_display = ContentDisplay()
        layout.addWidget(self.content_display)
        
        # Playlist manager
        self.playlist_manager = PlaylistManager(self._display_playlist_item)
        
        # Show initial message
        self.content_display.show_message("MakerScreen\nConnecting to server...")
    
    def connect_signals(self):
        """Connect thread-safe signals"""
        self.signals.content_update.connect(self._handle_content_update)
        self.signals.overlay_update.connect(self._handle_overlay_update)
        self.signals.show_message.connect(self.content_display.show_message)
    
    def _handle_content_update(self, content):
        """Handle content update from server"""
        content_type = content.get('type', 'image')
        
        if content_type.lower() == 'image':
            if 'data' in content:
                import base64
                image_data = base64.b64decode(content['data'])
                self.content_display.show_image(image_data)
            elif 'path' in content:
                self.content_display.show_image(content['path'])
        elif content_type.lower() == 'playlist':
            self.playlist_manager.set_playlist(content)
            self.playlist_manager.start()
    
    def _handle_overlay_update(self, overlay_config):
        """Handle overlay update from server"""
        overlay_id = overlay_config.get('id')
        
        if overlay_id in self.overlays:
            self.overlays[overlay_id].update_content(overlay_config.get('content', ''))
        else:
            overlay = OverlayWidget(overlay_config)
            overlay.setParent(self)
            overlay.update_content(overlay_config.get('content', ''))
            overlay.show()
            self.overlays[overlay_id] = overlay
    
    def _display_playlist_item(self, item):
        """Display a playlist item"""
        content_id = item.get('contentId')
        logger.info(f"Displaying playlist item: {content_id}")
        # Content would be loaded from cache or requested from server
    
    def show_fullscreen(self):
        """Show window in fullscreen mode"""
        self.showFullScreen()
        self.setCursor(Qt.BlankCursor)
    
    def keyPressEvent(self, event):
        """Handle key press events"""
        if event.key() == Qt.Key_Escape:
            self.close()
        elif event.key() == Qt.Key_F11:
            if self.isFullScreen():
                self.showNormal()
                self.setCursor(Qt.ArrowCursor)
            else:
                self.show_fullscreen()


def create_display():
    """Create and return the display application"""
    app = QApplication.instance()
    if app is None:
        app = QApplication(sys.argv)
    
    window = MainWindow()
    return app, window


if __name__ == '__main__':
    app, window = create_display()
    window.show_fullscreen()
    sys.exit(app.exec_())
