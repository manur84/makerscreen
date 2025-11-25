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
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "Installing MakerScreen Client..."
echo ""

# Create installation directory
echo "Creating installation directory..."
sudo mkdir -p "$INSTALL_DIR"
sudo mkdir -p "$INSTALL_DIR/content"
sudo chown -R $USER:$USER "$INSTALL_DIR"

# Copy files
echo "Copying client files..."
cp "$SCRIPT_DIR/client.py" "$INSTALL_DIR/"
cp "$SCRIPT_DIR/requirements.txt" "$INSTALL_DIR/"

# Copy optional files if they exist
[ -f "$SCRIPT_DIR/display_engine.py" ] && cp "$SCRIPT_DIR/display_engine.py" "$INSTALL_DIR/"
[ -f "$SCRIPT_DIR/web_ui.py" ] && cp "$SCRIPT_DIR/web_ui.py" "$INSTALL_DIR/"
[ -f "$SCRIPT_DIR/configure.sh" ] && cp "$SCRIPT_DIR/configure.sh" "$INSTALL_DIR/"

# Create default config if not exists
if [ ! -f "$INSTALL_DIR/config.json" ]; then
    echo '{"serverUrl":"ws://YOUR_SERVER_IP:8443","autoStart":true,"rotation":0,"brightness":100}' > "$INSTALL_DIR/config.json"
    echo "Created default config.json - please edit with your server IP"
fi

# Make scripts executable
chmod +x "$INSTALL_DIR/client.py"
[ -f "$INSTALL_DIR/configure.sh" ] && chmod +x "$INSTALL_DIR/configure.sh"

# Update and install system dependencies
echo "Updating package list..."
sudo apt-get update -qq

echo "Installing system dependencies..."
sudo apt-get install -y python3 python3-pip python3-venv python3-pyqt5 > /dev/null 2>&1 || {
    echo "Note: Some optional packages may not be available. Core functionality will still work."
}

# Create virtual environment
echo "Setting up Python virtual environment..."
python3 -m venv "$INSTALL_DIR/venv"
source "$INSTALL_DIR/venv/bin/activate"

# Install Python dependencies
echo "Installing Python dependencies..."
pip3 install --upgrade pip > /dev/null
pip3 install -r "$INSTALL_DIR/requirements.txt" > /dev/null

deactivate

# Install systemd service
echo "Installing systemd service..."
sudo cp "$SCRIPT_DIR/$SERVICE_NAME" /etc/systemd/system/

# Update service file to use virtual environment
sudo sed -i "s|ExecStart=.*|ExecStart=$INSTALL_DIR/venv/bin/python3 $INSTALL_DIR/client.py|" /etc/systemd/system/$SERVICE_NAME

sudo systemctl daemon-reload

# Enable service
echo "Enabling MakerScreen service..."
sudo systemctl enable "$SERVICE_NAME"

echo ""
echo "========================================="
echo "Installation Complete!"
echo "========================================="
echo ""
echo "Next steps:"
echo "1. Edit $INSTALL_DIR/config.json to set your server URL"
echo "   Run: nano $INSTALL_DIR/config.json"
echo ""
echo "2. Or use the configuration script:"
echo "   Run: $INSTALL_DIR/configure.sh"
echo ""
echo "3. Start the service:"
echo "   sudo systemctl start $SERVICE_NAME"
echo ""
echo "4. Check status:"
echo "   sudo systemctl status $SERVICE_NAME"
echo ""
echo "5. View logs:"
echo "   sudo journalctl -u $SERVICE_NAME -f"
echo ""
echo "6. Access Web UI at http://$(hostname -I | awk '{print $1}'):5001"
echo ""
echo "The service is set to start automatically on boot."
echo ""
