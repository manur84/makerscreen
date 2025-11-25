#!/bin/bash
# MakerScreen Configuration Setup Script
# Configures the server address and other settings

set -e

CONFIG_FILE="/opt/makerscreen/config.json"
BACKUP_FILE="/opt/makerscreen/config.json.bak"

echo "========================================="
echo "MakerScreen Client Configuration"
echo "========================================="
echo ""

# Check if running as appropriate user
if [ "$EUID" -eq 0 ]; then
    echo "Please run as a regular user (not root)."
    exit 1
fi

# Create backup of existing config
if [ -f "$CONFIG_FILE" ]; then
    cp "$CONFIG_FILE" "$BACKUP_FILE"
    echo "Backed up existing configuration to $BACKUP_FILE"
fi

# Get current values
CURRENT_URL=$(cat "$CONFIG_FILE" 2>/dev/null | grep -o '"serverUrl"[[:space:]]*:[[:space:]]*"[^"]*"' | cut -d'"' -f4 || echo "ws://localhost:8443")
CURRENT_NAME=$(cat "$CONFIG_FILE" 2>/dev/null | grep -o '"displayName"[[:space:]]*:[[:space:]]*"[^"]*"' | cut -d'"' -f4 || echo "")

echo "Current Configuration:"
echo "  Server URL: $CURRENT_URL"
echo "  Display Name: ${CURRENT_NAME:-Not set}"
echo ""

# Prompt for new values
read -p "Enter Server URL [$CURRENT_URL]: " SERVER_URL
SERVER_URL=${SERVER_URL:-$CURRENT_URL}

read -p "Enter Display Name [$CURRENT_NAME]: " DISPLAY_NAME
DISPLAY_NAME=${DISPLAY_NAME:-$CURRENT_NAME}

read -p "Enable Auto Start? (y/n) [y]: " AUTO_START
AUTO_START=${AUTO_START:-y}

if [[ "$AUTO_START" =~ ^[Yy]$ ]]; then
    AUTO_START_VALUE="true"
else
    AUTO_START_VALUE="false"
fi

read -p "Display Rotation (0, 90, 180, 270) [0]: " ROTATION
ROTATION=${ROTATION:-0}

read -p "Brightness (0-100) [100]: " BRIGHTNESS
BRIGHTNESS=${BRIGHTNESS:-100}

# Write configuration
cat > "$CONFIG_FILE" << EOF
{
    "serverUrl": "$SERVER_URL",
    "displayName": "$DISPLAY_NAME",
    "autoStart": $AUTO_START_VALUE,
    "rotation": $ROTATION,
    "brightness": $BRIGHTNESS,
    "version": "1.0.0"
}
EOF

echo ""
echo "Configuration saved to $CONFIG_FILE"
echo ""

# Ask to restart service
read -p "Restart MakerScreen service now? (y/n) [y]: " RESTART
RESTART=${RESTART:-y}

if [[ "$RESTART" =~ ^[Yy]$ ]]; then
    echo "Restarting service..."
    sudo systemctl restart makerscreen
    echo "Service restarted."
    echo ""
    echo "Check service status:"
    sudo systemctl status makerscreen --no-pager
fi

echo ""
echo "========================================="
echo "Configuration complete!"
echo "========================================="
echo ""
echo "Web UI available at: http://$(hostname -I | awk '{print $1}'):5001"
echo ""
