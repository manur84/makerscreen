#!/bin/bash
# Raspberry Pi Image Builder for MakerScreen
# Creates a custom Raspberry Pi OS image with MakerScreen client pre-installed

set -e

echo "========================================="
echo "MakerScreen Raspberry Pi Image Builder"
echo "========================================="
echo ""

# Configuration
BASE_IMAGE_URL="https://downloads.raspberrypi.org/raspios_lite_armhf/images/raspios_lite_armhf-2023-12-11/2023-12-11-raspios-bookworm-armhf-lite.img.xz"
OUTPUT_IMAGE="makerscreen-raspios.img"
WORK_DIR="./build"

# Create work directory
mkdir -p "$WORK_DIR"
cd "$WORK_DIR"

echo "Step 1: Downloading base Raspberry Pi OS image..."
if [ ! -f "base.img.xz" ]; then
    echo "Downloading from $BASE_IMAGE_URL..."
    wget -O base.img.xz "$BASE_IMAGE_URL"
else
    echo "Base image already downloaded."
fi

echo ""
echo "Step 2: Extracting image..."
if [ ! -f "base.img" ]; then
    xz -d -k base.img.xz
else
    echo "Image already extracted."
fi

echo ""
echo "Step 3: Mounting image..."
# Create mount point
sudo mkdir -p /mnt/raspi-boot
sudo mkdir -p /mnt/raspi-root

# Get partition offsets
BOOT_OFFSET=$(fdisk -l base.img | grep 'base.img1' | awk '{print $2}')
ROOT_OFFSET=$(fdisk -l base.img | grep 'base.img2' | awk '{print $2}')

# Mount partitions
sudo mount -o loop,offset=$((BOOT_OFFSET * 512)) base.img /mnt/raspi-boot
sudo mount -o loop,offset=$((ROOT_OFFSET * 512)) base.img /mnt/raspi-root

echo ""
echo "Step 4: Customizing image..."

# Enable SSH
sudo touch /mnt/raspi-boot/ssh

# Copy MakerScreen client files
sudo mkdir -p /mnt/raspi-root/opt/makerscreen
sudo cp ../../Client/RaspberryPi/client.py /mnt/raspi-root/opt/makerscreen/
sudo cp ../../Client/RaspberryPi/requirements.txt /mnt/raspi-root/opt/makerscreen/
sudo cp ../../Client/RaspberryPi/makerscreen.service /mnt/raspi-root/etc/systemd/system/

# Create default config
sudo bash -c 'cat > /mnt/raspi-root/opt/makerscreen/config.json << EOF
{
    "serverUrl": "ws://YOUR_SERVER_IP:8443",
    "autoStart": true
}
EOF'

# Create first-boot script to install dependencies and enable service
sudo bash -c 'cat > /mnt/raspi-root/etc/rc.local << "EOF"
#!/bin/bash
# First boot setup for MakerScreen

if [ ! -f /opt/makerscreen/.setup_complete ]; then
    echo "Running MakerScreen first-boot setup..."
    
    # Update and install dependencies
    apt-get update
    apt-get install -y python3 python3-pip
    
    # Install Python dependencies
    pip3 install -r /opt/makerscreen/requirements.txt
    
    # Enable service
    systemctl enable makerscreen
    systemctl start makerscreen
    
    # Mark setup as complete
    touch /opt/makerscreen/.setup_complete
    
    echo "MakerScreen setup complete!"
fi

exit 0
EOF'

sudo chmod +x /mnt/raspi-root/etc/rc.local

echo ""
echo "Step 5: Unmounting image..."
sudo umount /mnt/raspi-boot
sudo umount /mnt/raspi-root

echo ""
echo "Step 6: Compressing final image..."
cp base.img "../$OUTPUT_IMAGE"
cd ..
xz -z -k "$OUTPUT_IMAGE"

echo ""
echo "========================================="
echo "Image creation complete!"
echo "========================================="
echo ""
echo "Output image: $OUTPUT_IMAGE.xz"
echo ""
echo "To use this image:"
echo "1. Flash it to an SD card using Etcher or dd"
echo "2. Edit config.json on the boot partition to set your server IP"
echo "3. Insert SD card into Raspberry Pi and boot"
echo "4. The client will auto-start and connect to your server"
echo ""
