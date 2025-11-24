#!/bin/bash
# Network discovery and deployment script
# Discovers Raspberry Pi devices on the local network and deploys the client

set -e

echo "========================================="
echo "MakerScreen Auto-Discovery & Deployment"
echo "========================================="
echo ""

# Configuration
NETWORK_RANGE="192.168.1.0/24"  # Adjust for your network
SSH_USER="pi"
SSH_PASS="raspberry"  # Default password, should be changed
DEPLOYMENT_PACKAGE="./client-package.zip"

echo "Scanning network for Raspberry Pi devices..."
echo "Network range: $NETWORK_RANGE"
echo ""

# Scan network for devices
# This uses nmap to find devices with port 22 (SSH) open
DEVICES=$(sudo nmap -p 22 --open -oG - "$NETWORK_RANGE" | grep "22/open" | awk '{print $2}')

if [ -z "$DEVICES" ]; then
    echo "No devices found with SSH open."
    exit 0
fi

echo "Found devices:"
echo "$DEVICES"
echo ""

# Deploy to each device
for DEVICE in $DEVICES; do
    echo "========================================="
    echo "Deploying to $DEVICE..."
    echo "========================================="
    
    # Check if it's a Raspberry Pi by checking MAC address prefix
    MAC=$(arp -n "$DEVICE" | grep "$DEVICE" | awk '{print $3}')
    
    # Raspberry Pi MAC prefixes: b8:27:eb, dc:a6:32, e4:5f:01
    if [[ $MAC == b8:27:eb* ]] || [[ $MAC == dc:a6:32* ]] || [[ $MAC == e4:5f:01* ]]; then
        echo "Confirmed Raspberry Pi device (MAC: $MAC)"
        
        # Copy deployment package
        echo "Copying files..."
        sshpass -p "$SSH_PASS" scp -o StrictHostKeyChecking=no \
            ../../Client/RaspberryPi/client.py \
            ../../Client/RaspberryPi/requirements.txt \
            ../../Client/RaspberryPi/makerscreen.service \
            ../../Client/RaspberryPi/install.sh \
            "$SSH_USER@$DEVICE:/tmp/"
        
        # Run installation
        echo "Running installation..."
        sshpass -p "$SSH_PASS" ssh -o StrictHostKeyChecking=no \
            "$SSH_USER@$DEVICE" \
            "cd /tmp && chmod +x install.sh && ./install.sh"
        
        echo "Deployment to $DEVICE completed successfully!"
        echo ""
    else
        echo "Skipping $DEVICE (not a Raspberry Pi)"
        echo ""
    fi
done

echo "========================================="
echo "Auto-deployment complete!"
echo "========================================="
