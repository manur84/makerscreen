#!/bin/bash
set -e

echo "========================================="
echo "MakerScreen Client Installation"
echo "========================================="
echo ""

# Check if running as root
if [ "$EUID" -eq 0 ]; then 
    echo "Please do not run as root. The script will use sudo when needed."
    exit 1
fi

# Variables
INSTALL_DIR="/opt/makerscreen"
SERVICE_NAME="makerscreen.service"

echo "Installing MakerScreen Client..."
echo ""

# Create installation directory
echo "Creating installation directory..."
sudo mkdir -p "$INSTALL_DIR"
sudo chown -R $USER:$USER "$INSTALL_DIR"

# Copy files
echo "Copying client files..."
cp client.py "$INSTALL_DIR/"
cp config.json "$INSTALL_DIR/" 2>/dev/null || echo '{"serverUrl":"ws://YOUR_SERVER_IP:8443","autoStart":true}' > "$INSTALL_DIR/config.json"
cp requirements.txt "$INSTALL_DIR/"

# Make client executable
chmod +x "$INSTALL_DIR/client.py"

# Install Python dependencies
echo "Installing Python dependencies..."
pip3 install -r "$INSTALL_DIR/requirements.txt"

# Install systemd service
echo "Installing systemd service..."
sudo cp "$SERVICE_NAME" /etc/systemd/system/
sudo systemctl daemon-reload

# Enable and start service
echo "Enabling MakerScreen service..."
sudo systemctl enable "$SERVICE_NAME"

echo ""
echo "========================================="
echo "Installation Complete!"
echo "========================================="
echo ""
echo "Next steps:"
echo "1. Edit $INSTALL_DIR/config.json to set your server URL"
echo "2. Start the service with: sudo systemctl start $SERVICE_NAME"
echo "3. Check status with: sudo systemctl status $SERVICE_NAME"
echo "4. View logs with: sudo journalctl -u $SERVICE_NAME -f"
echo ""
echo "To start automatically on boot, the service is already enabled."
echo ""
