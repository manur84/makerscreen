#!/usr/bin/env python3
"""
MakerScreen Web UI - Local configuration interface
"""

from flask import Flask, render_template, request, jsonify
import json
import subprocess
import socket

app = Flask(__name__)

@app.route('/')
def index():
    """Main dashboard"""
    return render_template('dashboard.html', 
                         hostname=socket.gethostname(),
                         ip=get_ip_address())

@app.route('/api/status')
def status():
    """Get client status"""
    return jsonify({
        'hostname': socket.gethostname(),
        'ip': get_ip_address(),
        'status': 'online',
        'uptime': get_uptime()
    })

@app.route('/api/network', methods=['GET', 'POST'])
def network():
    """Network configuration"""
    if request.method == 'POST':
        # TODO: Update network settings
        return jsonify({'success': True})
    
    return jsonify({
        'interface': 'eth0',
        'ip': get_ip_address()
    })

@app.route('/api/reboot', methods=['POST'])
def reboot():
    """Reboot system"""
    try:
        subprocess.run(['sudo', 'reboot'], check=True)
        return jsonify({'success': True})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})

def get_ip_address():
    """Get IP address"""
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except:
        return "0.0.0.0"

def get_uptime():
    """Get system uptime"""
    try:
        with open('/proc/uptime', 'r') as f:
            uptime_seconds = float(f.readline().split()[0])
            return int(uptime_seconds)
    except:
        return 0

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8080, debug=False)
