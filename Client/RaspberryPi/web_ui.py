#!/usr/bin/env python3
"""
MakerScreen Local Web UI
Provides a local web interface for client configuration and status
"""

from flask import Flask, render_template_string, jsonify, request, redirect, url_for
import json
import os
import platform
import psutil
import socket
import subprocess
import logging
from datetime import datetime

logger = logging.getLogger('WebUI')

app = Flask(__name__)
app.secret_key = os.urandom(24)

CONFIG_FILE = '/opt/makerscreen/config.json'
CONTENT_DIR = '/opt/makerscreen/content'

# HTML Templates
BASE_TEMPLATE = '''
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MakerScreen - {{ title }}</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: 'Segoe UI', Arial, sans-serif;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
            min-height: 100vh;
            color: #e0e0e0;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
        }
        header {
            background: rgba(255,255,255,0.1);
            padding: 20px;
            margin-bottom: 20px;
            border-radius: 10px;
        }
        header h1 {
            color: #2196F3;
            font-size: 28px;
        }
        header p {
            color: #888;
        }
        .card {
            background: rgba(255,255,255,0.05);
            border-radius: 10px;
            padding: 20px;
            margin-bottom: 20px;
            border: 1px solid rgba(255,255,255,0.1);
        }
        .card h2 {
            color: #2196F3;
            margin-bottom: 15px;
            font-size: 20px;
        }
        .status-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
        }
        .status-item {
            background: rgba(0,0,0,0.2);
            padding: 15px;
            border-radius: 8px;
        }
        .status-item label {
            display: block;
            color: #888;
            font-size: 12px;
            margin-bottom: 5px;
        }
        .status-item .value {
            font-size: 18px;
            font-weight: 600;
        }
        .status-online {
            color: #4CAF50;
        }
        .status-offline {
            color: #F44336;
        }
        .form-group {
            margin-bottom: 15px;
        }
        .form-group label {
            display: block;
            margin-bottom: 5px;
            color: #aaa;
        }
        .form-group input, .form-group select {
            width: 100%;
            padding: 10px;
            border: 1px solid rgba(255,255,255,0.2);
            border-radius: 5px;
            background: rgba(0,0,0,0.3);
            color: #fff;
            font-size: 16px;
        }
        .btn {
            display: inline-block;
            padding: 10px 20px;
            background: #2196F3;
            color: white;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-size: 16px;
            margin-right: 10px;
        }
        .btn:hover {
            background: #1976D2;
        }
        .btn-danger {
            background: #F44336;
        }
        .btn-danger:hover {
            background: #D32F2F;
        }
        .btn-success {
            background: #4CAF50;
        }
        .btn-success:hover {
            background: #388E3C;
        }
        nav {
            margin-bottom: 20px;
        }
        nav a {
            color: #2196F3;
            text-decoration: none;
            margin-right: 20px;
        }
        nav a:hover {
            text-decoration: underline;
        }
        .progress-bar {
            height: 10px;
            background: rgba(255,255,255,0.1);
            border-radius: 5px;
            overflow: hidden;
            margin-top: 5px;
        }
        .progress-bar .fill {
            height: 100%;
            background: #2196F3;
            border-radius: 5px;
        }
        .log-output {
            background: #000;
            padding: 15px;
            border-radius: 5px;
            font-family: monospace;
            font-size: 12px;
            max-height: 400px;
            overflow-y: auto;
            white-space: pre-wrap;
        }
        table {
            width: 100%;
            border-collapse: collapse;
        }
        th, td {
            padding: 10px;
            text-align: left;
            border-bottom: 1px solid rgba(255,255,255,0.1);
        }
        th {
            color: #888;
        }
        .qr-code {
            text-align: center;
            padding: 20px;
        }
        .qr-code img {
            max-width: 200px;
            background: white;
            padding: 10px;
            border-radius: 10px;
        }
    </style>
</head>
<body>
    <div class="container">
        <header>
            <h1>🖥️ MakerScreen</h1>
            <p>Digital Signage Client</p>
        </header>
        <nav>
            <a href="/">Dashboard</a>
            <a href="/config">Configuration</a>
            <a href="/content">Content</a>
            <a href="/logs">Logs</a>
            <a href="/system">System</a>
        </nav>
        {% block content %}{% endblock %}
    </div>
</body>
</html>
'''

DASHBOARD_TEMPLATE = BASE_TEMPLATE.replace('{% block content %}{% endblock %}', '''
{% block content %}
<div class="card">
    <h2>Status</h2>
    <div class="status-grid">
        <div class="status-item">
            <label>Connection</label>
            <div class="value {{ 'status-online' if status.connected else 'status-offline' }}">
                {{ 'Connected' if status.connected else 'Disconnected' }}
            </div>
        </div>
        <div class="status-item">
            <label>Client ID</label>
            <div class="value">{{ status.client_id[:12] }}...</div>
        </div>
        <div class="status-item">
            <label>Server</label>
            <div class="value">{{ status.server_url }}</div>
        </div>
        <div class="status-item">
            <label>Uptime</label>
            <div class="value">{{ status.uptime }}</div>
        </div>
    </div>
</div>

<div class="card">
    <h2>System Resources</h2>
    <div class="status-grid">
        <div class="status-item">
            <label>CPU Usage</label>
            <div class="value">{{ system.cpu_percent }}%</div>
            <div class="progress-bar"><div class="fill" style="width: {{ system.cpu_percent }}%"></div></div>
        </div>
        <div class="status-item">
            <label>Memory Usage</label>
            <div class="value">{{ system.memory_percent }}%</div>
            <div class="progress-bar"><div class="fill" style="width: {{ system.memory_percent }}%"></div></div>
        </div>
        <div class="status-item">
            <label>Disk Usage</label>
            <div class="value">{{ system.disk_percent }}%</div>
            <div class="progress-bar"><div class="fill" style="width: {{ system.disk_percent }}%"></div></div>
        </div>
        <div class="status-item">
            <label>Temperature</label>
            <div class="value">{{ system.temperature }}°C</div>
        </div>
    </div>
</div>

<div class="card">
    <h2>Network</h2>
    <div class="status-grid">
        <div class="status-item">
            <label>IP Address</label>
            <div class="value">{{ network.ip_address }}</div>
        </div>
        <div class="status-item">
            <label>MAC Address</label>
            <div class="value">{{ network.mac_address }}</div>
        </div>
        <div class="status-item">
            <label>Hostname</label>
            <div class="value">{{ network.hostname }}</div>
        </div>
    </div>
</div>

<div class="card qr-code">
    <h2>Quick Access QR Code</h2>
    <p>Scan to access this page from your phone</p>
    <img src="/api/qrcode" alt="QR Code">
</div>
{% endblock %}
''')

CONFIG_TEMPLATE = BASE_TEMPLATE.replace('{% block content %}{% endblock %}', '''
{% block content %}
<div class="card">
    <h2>Server Configuration</h2>
    <form method="POST" action="/config">
        <div class="form-group">
            <label>Server URL</label>
            <input type="text" name="serverUrl" value="{{ config.serverUrl }}" placeholder="ws://server-ip:8443">
        </div>
        <div class="form-group">
            <label>Auto Start</label>
            <select name="autoStart">
                <option value="true" {{ 'selected' if config.autoStart else '' }}>Enabled</option>
                <option value="false" {{ 'selected' if not config.autoStart else '' }}>Disabled</option>
            </select>
        </div>
        <div class="form-group">
            <label>Display Name</label>
            <input type="text" name="displayName" value="{{ config.displayName or '' }}" placeholder="Living Room Display">
        </div>
        <button type="submit" class="btn btn-success">Save Configuration</button>
    </form>
</div>

<div class="card">
    <h2>Display Settings</h2>
    <form method="POST" action="/config/display">
        <div class="form-group">
            <label>Rotation</label>
            <select name="rotation">
                <option value="0" {{ 'selected' if config.rotation == 0 else '' }}>Normal (0°)</option>
                <option value="90" {{ 'selected' if config.rotation == 90 else '' }}>90°</option>
                <option value="180" {{ 'selected' if config.rotation == 180 else '' }}>180°</option>
                <option value="270" {{ 'selected' if config.rotation == 270 else '' }}>270°</option>
            </select>
        </div>
        <div class="form-group">
            <label>Brightness (0-100)</label>
            <input type="number" name="brightness" value="{{ config.brightness or 100 }}" min="0" max="100">
        </div>
        <button type="submit" class="btn btn-success">Save Display Settings</button>
    </form>
</div>
{% endblock %}
''')

CONTENT_TEMPLATE = BASE_TEMPLATE.replace('{% block content %}{% endblock %}', '''
{% block content %}
<div class="card">
    <h2>Cached Content</h2>
    {% if content_files %}
    <table>
        <thead>
            <tr>
                <th>Filename</th>
                <th>Size</th>
                <th>Modified</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            {% for file in content_files %}
            <tr>
                <td>{{ file.name }}</td>
                <td>{{ file.size }}</td>
                <td>{{ file.modified }}</td>
                <td>
                    <a href="/content/preview/{{ file.name }}" class="btn">Preview</a>
                    <form method="POST" action="/content/delete/{{ file.name }}" style="display:inline">
                        <button type="submit" class="btn btn-danger">Delete</button>
                    </form>
                </td>
            </tr>
            {% endfor %}
        </tbody>
    </table>
    {% else %}
    <p>No cached content found.</p>
    {% endif %}
</div>

<div class="card">
    <h2>Actions</h2>
    <form method="POST" action="/content/clear">
        <button type="submit" class="btn btn-danger">Clear All Content</button>
    </form>
</div>
{% endblock %}
''')

LOGS_TEMPLATE = BASE_TEMPLATE.replace('{% block content %}{% endblock %}', '''
{% block content %}
<div class="card">
    <h2>Application Logs</h2>
    <div class="log-output">{{ logs }}</div>
</div>

<div class="card">
    <h2>Actions</h2>
    <form method="POST" action="/logs/clear" style="display:inline">
        <button type="submit" class="btn btn-danger">Clear Logs</button>
    </form>
    <a href="/api/logs/download" class="btn">Download Logs</a>
</div>
{% endblock %}
''')

SYSTEM_TEMPLATE = BASE_TEMPLATE.replace('{% block content %}{% endblock %}', '''
{% block content %}
<div class="card">
    <h2>System Information</h2>
    <div class="status-grid">
        <div class="status-item">
            <label>Platform</label>
            <div class="value">{{ system.platform }}</div>
        </div>
        <div class="status-item">
            <label>Python Version</label>
            <div class="value">{{ system.python_version }}</div>
        </div>
        <div class="status-item">
            <label>Client Version</label>
            <div class="value">{{ system.client_version }}</div>
        </div>
        <div class="status-item">
            <label>Boot Time</label>
            <div class="value">{{ system.boot_time }}</div>
        </div>
    </div>
</div>

<div class="card">
    <h2>Service Control</h2>
    <button class="btn btn-success" onclick="restartService()">Restart Service</button>
    <button class="btn" onclick="restartDisplay()">Restart Display</button>
    <button class="btn btn-danger" onclick="rebootSystem()">Reboot System</button>
</div>

<div class="card">
    <h2>Network Tools</h2>
    <form method="POST" action="/system/test-connection">
        <div class="form-group">
            <label>Test Server Connection</label>
            <input type="text" name="server" placeholder="Server URL" value="{{ config.serverUrl }}">
        </div>
        <button type="submit" class="btn">Test Connection</button>
    </form>
</div>

<script>
function restartService() {
    if(confirm('Restart the MakerScreen service?')) {
        fetch('/api/restart-service', {method: 'POST'})
            .then(() => location.reload());
    }
}
function restartDisplay() {
    if(confirm('Restart the display?')) {
        fetch('/api/restart-display', {method: 'POST'})
            .then(() => location.reload());
    }
}
function rebootSystem() {
    if(confirm('Reboot the entire system?')) {
        fetch('/api/reboot', {method: 'POST'})
            .then(() => alert('System is rebooting...'));
    }
}
</script>
{% endblock %}
''')


def load_config():
    """Load configuration from file"""
    try:
        if os.path.exists(CONFIG_FILE):
            with open(CONFIG_FILE, 'r') as f:
                return json.load(f)
    except Exception as e:
        logger.error(f"Error loading config: {e}")
    return {
        'serverUrl': 'ws://localhost:8443',
        'autoStart': True,
        'rotation': 0,
        'brightness': 100
    }


def save_config(config):
    """Save configuration to file"""
    try:
        os.makedirs(os.path.dirname(CONFIG_FILE), exist_ok=True)
        with open(CONFIG_FILE, 'w') as f:
            json.dump(config, f, indent=2)
        return True
    except Exception as e:
        logger.error(f"Error saving config: {e}")
        return False


def get_system_info():
    """Get system information"""
    try:
        cpu_percent = psutil.cpu_percent(interval=0.1)
        memory = psutil.virtual_memory()
        disk = psutil.disk_usage('/')
        
        # Get temperature (Raspberry Pi specific)
        temperature = 0
        try:
            if os.path.exists('/sys/class/thermal/thermal_zone0/temp'):
                with open('/sys/class/thermal/thermal_zone0/temp', 'r') as f:
                    temperature = int(f.read()) / 1000
        except:
            pass
        
        return {
            'cpu_percent': round(cpu_percent, 1),
            'memory_percent': round(memory.percent, 1),
            'disk_percent': round(disk.percent, 1),
            'temperature': round(temperature, 1),
            'platform': platform.platform(),
            'python_version': platform.python_version(),
            'client_version': '1.0.0',
            'boot_time': datetime.fromtimestamp(psutil.boot_time()).strftime('%Y-%m-%d %H:%M:%S')
        }
    except Exception as e:
        logger.error(f"Error getting system info: {e}")
        return {}


def get_network_info():
    """Get network information"""
    try:
        hostname = socket.gethostname()
        ip_address = socket.gethostbyname(hostname)
        
        # Get MAC address
        mac_address = 'Unknown'
        try:
            import netifaces
            for iface in netifaces.interfaces():
                addrs = netifaces.ifaddresses(iface)
                if netifaces.AF_LINK in addrs:
                    mac = addrs[netifaces.AF_LINK][0].get('addr', '')
                    if mac and mac != '00:00:00:00:00:00':
                        mac_address = mac
                        break
        except:
            pass
        
        return {
            'hostname': hostname,
            'ip_address': ip_address,
            'mac_address': mac_address
        }
    except Exception as e:
        logger.error(f"Error getting network info: {e}")
        return {'hostname': 'Unknown', 'ip_address': 'Unknown', 'mac_address': 'Unknown'}


def get_status():
    """Get client status"""
    config = load_config()
    return {
        'connected': False,  # Would be updated by actual client
        'client_id': get_network_info().get('mac_address', 'Unknown').replace(':', ''),
        'server_url': config.get('serverUrl', 'Not configured'),
        'uptime': 'N/A'
    }


def get_content_files():
    """List cached content files"""
    files = []
    try:
        if os.path.exists(CONTENT_DIR):
            for filename in os.listdir(CONTENT_DIR):
                filepath = os.path.join(CONTENT_DIR, filename)
                if os.path.isfile(filepath):
                    stat = os.stat(filepath)
                    files.append({
                        'name': filename,
                        'size': f"{stat.st_size / 1024:.1f} KB",
                        'modified': datetime.fromtimestamp(stat.st_mtime).strftime('%Y-%m-%d %H:%M')
                    })
    except Exception as e:
        logger.error(f"Error listing content: {e}")
    return files


@app.route('/')
def dashboard():
    return render_template_string(
        DASHBOARD_TEMPLATE,
        title='Dashboard',
        status=get_status(),
        system=get_system_info(),
        network=get_network_info()
    )


@app.route('/config', methods=['GET', 'POST'])
def config():
    current_config = load_config()
    
    if request.method == 'POST':
        current_config['serverUrl'] = request.form.get('serverUrl', current_config.get('serverUrl'))
        current_config['autoStart'] = request.form.get('autoStart') == 'true'
        current_config['displayName'] = request.form.get('displayName', '')
        save_config(current_config)
        return redirect(url_for('config'))
    
    return render_template_string(CONFIG_TEMPLATE, title='Configuration', config=current_config)


@app.route('/config/display', methods=['POST'])
def config_display():
    current_config = load_config()
    current_config['rotation'] = int(request.form.get('rotation', 0))
    current_config['brightness'] = int(request.form.get('brightness', 100))
    save_config(current_config)
    return redirect(url_for('config'))


@app.route('/content')
def content():
    return render_template_string(
        CONTENT_TEMPLATE,
        title='Content',
        content_files=get_content_files()
    )


@app.route('/content/clear', methods=['POST'])
def clear_content():
    try:
        if os.path.exists(CONTENT_DIR):
            for filename in os.listdir(CONTENT_DIR):
                filepath = os.path.join(CONTENT_DIR, filename)
                if os.path.isfile(filepath):
                    os.remove(filepath)
    except Exception as e:
        logger.error(f"Error clearing content: {e}")
    return redirect(url_for('content'))


@app.route('/content/delete/<filename>', methods=['POST'])
def delete_content(filename):
    try:
        filepath = os.path.join(CONTENT_DIR, filename)
        if os.path.exists(filepath):
            os.remove(filepath)
    except Exception as e:
        logger.error(f"Error deleting content: {e}")
    return redirect(url_for('content'))


@app.route('/logs')
def logs():
    log_content = "No logs available"
    try:
        result = subprocess.run(
            ['journalctl', '-u', 'makerscreen', '-n', '100', '--no-pager'],
            capture_output=True, text=True
        )
        if result.stdout:
            log_content = result.stdout
    except Exception as e:
        log_content = f"Error reading logs: {e}"
    
    return render_template_string(LOGS_TEMPLATE, title='Logs', logs=log_content)


@app.route('/system')
def system():
    return render_template_string(
        SYSTEM_TEMPLATE,
        title='System',
        system=get_system_info(),
        config=load_config()
    )


@app.route('/api/status')
def api_status():
    return jsonify({
        'status': get_status(),
        'system': get_system_info(),
        'network': get_network_info()
    })


@app.route('/api/qrcode')
def api_qrcode():
    try:
        import qrcode
        import io
        
        network = get_network_info()
        url = f"http://{network['ip_address']}:5001"
        
        qr = qrcode.QRCode(version=1, box_size=10, border=5)
        qr.add_data(url)
        qr.make(fit=True)
        
        img = qr.make_image(fill_color="black", back_color="white")
        
        buffer = io.BytesIO()
        img.save(buffer, format='PNG')
        buffer.seek(0)
        
        from flask import send_file
        return send_file(buffer, mimetype='image/png')
    except Exception as e:
        logger.error(f"Error generating QR code: {e}")
        return "QR generation failed", 500


@app.route('/api/restart-service', methods=['POST'])
def api_restart_service():
    try:
        subprocess.run(['sudo', 'systemctl', 'restart', 'makerscreen'], check=True)
        return jsonify({'success': True})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


@app.route('/api/restart-display', methods=['POST'])
def api_restart_display():
    # Would restart the display component
    return jsonify({'success': True})


@app.route('/api/reboot', methods=['POST'])
def api_reboot():
    try:
        subprocess.Popen(['sudo', 'reboot'])
        return jsonify({'success': True})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


def run_webui(port=5001):
    """Run the web UI server"""
    app.run(host='0.0.0.0', port=port, debug=False, threaded=True)


if __name__ == '__main__':
    run_webui()
