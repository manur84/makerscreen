# MakerScreen Security Best Practices

## Table of Contents
1. [Security Overview](#security-overview)
2. [Network Security](#network-security)
3. [Certificate Management](#certificate-management)
4. [Authentication & Authorization](#authentication--authorization)
5. [Data Protection](#data-protection)
6. [Audit & Compliance](#audit--compliance)
7. [Security Operations](#security-operations)
8. [Incident Response](#incident-response)

## Security Overview

MakerScreen is designed with a **security-first** approach, implementing defense-in-depth strategies across all system components. The system operates exclusively within isolated corporate networks with no Internet exposure, utilizing mutual TLS authentication and comprehensive audit logging.

### Security Principles

1. **Network Isolation**: Complete air-gap from Internet
2. **Encryption Everywhere**: TLS 1.3 for all communications
3. **Mutual Authentication**: Both client and server verify identities
4. **Principle of Least Privilege**: Minimal permissions for all components
5. **Defense in Depth**: Multiple security layers
6. **Security by Default**: Secure configuration out-of-the-box
7. **Zero Trust**: Verify everything, trust nothing
8. **Audit Everything**: Complete trail of all activities

### Threat Model

**In-Scope Threats:**
- Internal network attacks
- Compromised client devices
- Unauthorized access attempts
- Certificate theft
- Man-in-the-middle attacks
- Privilege escalation
- Data exfiltration
- Denial of service

**Out-of-Scope:**
- Internet-based attacks (network isolated)
- Physical tampering (assume physical security)
- Social engineering (user training required)

---

## Network Security

### Network Isolation

**Air-Gap Strategy:**
```
┌─────────────────────────────────────────┐
│     Corporate Network (ISOLATED)         │
│  ┌──────────────────────────────────┐   │
│  │  MakerScreen Infrastructure      │   │
│  │  - Server: 10.0.1.10             │   │
│  │  - Clients: 10.0.2.0/24          │   │
│  │  - Admin: 10.0.1.0/24            │   │
│  └──────────────────────────────────┘   │
│           ┌──────────┐                   │
│           │ Firewall │                   │
│           │ DENY ALL │                   │
│           └──────────┘                   │
└─────────────────────────────────────────┘
              │ BLOCKED
              ▼
       ┌────────────┐
       │  Internet  │
       │ (NO ACCESS)│
       └────────────┘
```

**Implementation:**
- Physical network segmentation (VLANs)
- Firewall rules blocking all outbound Internet traffic
- No routes to Internet gateway
- DNS limited to internal resolution only
- NTP from internal time server

### VLAN Segmentation

**Recommended VLAN Structure:**

| VLAN ID | Name | Subnet | Purpose | Access |
|---------|------|--------|---------|--------|
| 10 | Management | 10.0.1.0/24 | Servers, Admin PCs | Full control |
| 20 | Display | 10.0.2.0/24 | Raspberry Pi clients | Restricted |
| 30 | Guest | 10.0.3.0/24 | iOS devices (optional) | Limited |

**Inter-VLAN Firewall Rules:**

```
# Management → Server
Allow TCP 8443 (WSS)
Allow TCP 8080 (REST API)
Allow TCP 1433 (SQL Server - admin only)
Allow TCP 3389 (RDP - admin only)

# Display → Server
Allow TCP 8443 (WSS)
Allow UDP 5353 (mDNS)
Deny ALL others

# Guest → Server
Allow TCP 8443 (WSS via VPN)
Allow TCP 8080 (REST API)
Deny ALL others

# All → Internet
DENY ALL
```

### Port Security

**Required Ports:**

| Port | Protocol | Service | Access |
|------|----------|---------|--------|
| 8443 | TCP | WebSocket Secure (WSS) | All authorized devices |
| 8080 | TCP | REST API | Admin, iOS |
| 5353 | UDP | mDNS | Internal discovery |
| 1433 | TCP | SQL Server | Admin only |
| 3389 | TCP | RDP | Admin only |

**Security Recommendations:**
- Change default ports in production
- Use port knocking for admin ports
- Implement rate limiting on all ports
- Monitor for port scans
- Disable unused ports

### Network Monitoring

**Continuous Monitoring:**
- Network traffic analysis (Wireshark, tcpdump)
- Intrusion detection (Snort, Suricata)
- Anomaly detection (unusual traffic patterns)
- Connection logging
- Bandwidth monitoring

**Alerts:**
- Unauthorized connection attempts
- Port scanning activity
- Unusual traffic volumes
- Failed authentication attempts
- Certificate validation failures

---

## Certificate Management

### Certificate Hierarchy

```
┌────────────────────────────────────┐
│  Root Certificate Authority (CA)   │
│  - Offline storage (HSM/USB)       │
│  - 10-year validity                │
│  - Signs intermediate CA only      │
└──────────────┬─────────────────────┘
               │
               ▼
┌────────────────────────────────────┐
│  Intermediate CA                   │
│  - Online, secured server          │
│  - 5-year validity                 │
│  - Signs server & client certs     │
└──────────────┬─────────────────────┘
               │
       ┌───────┴────────┐
       ▼                ▼
┌─────────────┐  ┌─────────────────┐
│   Server    │  │  Client Certs   │
│   Cert      │  │  (Per device)   │
│  2-year     │  │  2-year         │
└─────────────┘  └─────────────────┘
```

### Certificate Generation

**Root CA Generation:**

```bash
# Generate Root CA private key (offline)
openssl genrsa -aes256 -out root-ca-key.pem 4096

# Create Root CA certificate
openssl req -new -x509 -days 3650 -key root-ca-key.pem \
  -out root-ca-cert.pem \
  -subj "/C=DE/O=Company/CN=MakerScreen Root CA"

# Secure the root key
chmod 400 root-ca-key.pem
# Store on encrypted USB drive, keep offline
```

**Intermediate CA Generation:**

```bash
# Generate Intermediate CA private key
openssl genrsa -aes256 -out intermediate-ca-key.pem 4096

# Create certificate signing request
openssl req -new -key intermediate-ca-key.pem \
  -out intermediate-ca-csr.pem \
  -subj "/C=DE/O=Company/CN=MakerScreen Intermediate CA"

# Sign with Root CA (offline operation)
openssl x509 -req -days 1825 \
  -in intermediate-ca-csr.pem \
  -CA root-ca-cert.pem \
  -CAkey root-ca-key.pem \
  -CAcreateserial \
  -out intermediate-ca-cert.pem \
  -extensions v3_ca

# Create certificate chain
cat intermediate-ca-cert.pem root-ca-cert.pem > ca-chain.pem
```

**Server Certificate:**

```bash
# Generate server private key
openssl genrsa -out server-key.pem 2048

# Create CSR
openssl req -new -key server-key.pem \
  -out server-csr.pem \
  -subj "/C=DE/O=Company/CN=makerscreen-server.local"

# Create extension file
cat > server-ext.cnf << EOF
subjectAltName = DNS:makerscreen-server.local,DNS:*.makerscreen.local,IP:10.0.1.10
extendedKeyUsage = serverAuth
keyUsage = digitalSignature, keyEncipherment
EOF

# Sign with Intermediate CA
openssl x509 -req -days 730 \
  -in server-csr.pem \
  -CA intermediate-ca-cert.pem \
  -CAkey intermediate-ca-key.pem \
  -CAcreateserial \
  -out server-cert.pem \
  -extfile server-ext.cnf

# Create PFX for Windows
openssl pkcs12 -export -out server-cert.pfx \
  -inkey server-key.pem \
  -in server-cert.pem \
  -certfile ca-chain.pem
```

**Client Certificate:**

```bash
# Generate client private key
MAC_ADDRESS="b827eb123456"
openssl genrsa -out client-${MAC_ADDRESS}-key.pem 2048

# Create CSR
openssl req -new -key client-${MAC_ADDRESS}-key.pem \
  -out client-${MAC_ADDRESS}-csr.pem \
  -subj "/C=DE/O=Company/CN=client-${MAC_ADDRESS}"

# Create extension file
cat > client-ext.cnf << EOF
extendedKeyUsage = clientAuth
keyUsage = digitalSignature
EOF

# Sign with Intermediate CA
openssl x509 -req -days 730 \
  -in client-${MAC_ADDRESS}-csr.pem \
  -CA intermediate-ca-cert.pem \
  -CAkey intermediate-ca-key.pem \
  -CAcreateserial \
  -out client-${MAC_ADDRESS}-cert.pem \
  -extfile client-ext.cnf
```

### Certificate Storage

**Server:**
- Location: `C:\ProgramData\MakerScreen\Certificates\`
- Permissions: SYSTEM and Administrators only
- Encryption: Windows Data Protection API (DPAPI)
- Backup: Encrypted backup daily

**Client (Raspberry Pi):**
- Location: `/etc/makerscreen/certs/`
- Permissions: `root:root 400` (read-only by root)
- Embedded in image during generation
- No private key export capability

**iOS:**
- Location: Keychain
- Access: App-specific, biometric protected
- Distribution: MDM or manual import
- Revocation: CRL check on connection

### Certificate Lifecycle

**Renewal Process:**

1. **Automated Renewal (90 days before expiry):**
   ```
   Server checks certificate expiry → 
   Generate new CSR → 
   Auto-sign with Intermediate CA → 
   Deploy new certificate → 
   Notify administrators
   ```

2. **Client Certificate Renewal:**
   ```
   Server detects expiring client cert → 
   Generate new certificate → 
   Push to client via WSS → 
   Client installs and reloads → 
   Confirm successful renewal
   ```

3. **Manual Renewal:**
   - Server admin portal provides renewal interface
   - One-click renewal for all certificates
   - Batch renewal for clients

**Revocation:**

1. **Certificate Revocation List (CRL):**
   ```
   Server maintains CRL at:
   http://makerscreen-server.local/crl.pem
   
   Clients check CRL on:
   - Initial connection
   - Every 24 hours
   - On-demand via admin command
   ```

2. **Revocation Process:**
   ```
   Admin identifies compromised certificate → 
   Add to CRL via management interface → 
   Publish updated CRL → 
   Notify all clients → 
   Clients disconnect affected certificates → 
   Issue replacement certificates
   ```

3. **Online Certificate Status Protocol (OCSP):**
   - Optional OCSP responder for real-time validation
   - Provides instant revocation status
   - Reduces latency vs CRL downloads

### Certificate Validation

**Server-Side Validation:**

```csharp
// C# Server Certificate Validation
public bool ValidateClientCertificate(X509Certificate2 clientCert)
{
    // 1. Check certificate is not null
    if (clientCert == null)
        return false;
    
    // 2. Verify certificate chain
    using var chain = new X509Chain();
    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
    chain.ChainPolicy.ExtraStore.Add(intermediateCACert);
    chain.ChainPolicy.ExtraStore.Add(rootCACert);
    
    if (!chain.Build(clientCert))
        return false;
    
    // 3. Check validity period
    if (DateTime.Now < clientCert.NotBefore || 
        DateTime.Now > clientCert.NotAfter)
        return false;
    
    // 4. Verify extended key usage
    var ekuOid = new Oid("1.3.6.1.5.5.7.3.2"); // Client Authentication
    if (!clientCert.Extensions.OfType<X509EnhancedKeyUsageExtension>()
        .Any(e => e.EnhancedKeyUsages.Cast<Oid>().Any(o => o.Value == ekuOid.Value)))
        return false;
    
    // 5. Check against CRL
    if (IsCertificateRevoked(clientCert))
        return false;
    
    // 6. Validate subject matches expected pattern
    if (!Regex.IsMatch(clientCert.Subject, @"CN=client-[0-9a-f]{12}"))
        return false;
    
    return true;
}
```

**Client-Side Validation:**

```python
# Python Client Certificate Validation
def validate_server_certificate(cert):
    # 1. Verify certificate chain
    store = ssl.create_default_context()
    store.load_verify_locations('/etc/makerscreen/certs/ca-chain.pem')
    
    try:
        ssl_sock = store.wrap_socket(...)
    except ssl.SSLError:
        return False
    
    # 2. Check hostname matches
    ssl.match_hostname(cert, 'makerscreen-server.local')
    
    # 3. Verify validity period
    not_before = datetime.strptime(cert['notBefore'], '%b %d %H:%M:%S %Y %Z')
    not_after = datetime.strptime(cert['notAfter'], '%b %d %H:%M:%S %Y %Z')
    
    if not (not_before <= datetime.now() <= not_after):
        return False
    
    # 4. Check extended key usage
    for ext in cert.get('extensions', []):
        if ext[0] == 'extendedKeyUsage':
            if 'TLS Web Server Authentication' not in ext[1]:
                return False
    
    return True
```

---

## Authentication & Authorization

### Multi-Factor Authentication

**Authentication Methods:**

1. **Windows Server Management:**
   - Primary: Active Directory credentials
   - Secondary: TOTP (Time-based One-Time Password)
   - Optional: Smart card / PIV

2. **iOS Application:**
   - Primary: Username/Password
   - Secondary: Biometric (Face ID / Touch ID)
   - Optional: Certificate-based auth

3. **Raspberry Pi Clients:**
   - Primary: Client certificate (mutual TLS)
   - Secondary: Device registration token
   - No user interaction required

### Role-Based Access Control (RBAC)

**Role Definitions:**

```sql
-- SQL Server RBAC Schema
CREATE TABLE Roles (
    RoleID INT PRIMARY KEY IDENTITY,
    RoleName NVARCHAR(50) UNIQUE NOT NULL,
    Description NVARCHAR(500),
    Created DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Permissions (
    PermissionID INT PRIMARY KEY IDENTITY,
    PermissionName NVARCHAR(100) UNIQUE NOT NULL,
    Resource NVARCHAR(100) NOT NULL,
    Action NVARCHAR(50) NOT NULL, -- CREATE, READ, UPDATE, DELETE, EXECUTE
    Description NVARCHAR(500)
);

CREATE TABLE RolePermissions (
    RoleID INT FOREIGN KEY REFERENCES Roles(RoleID),
    PermissionID INT FOREIGN KEY REFERENCES Permissions(PermissionID),
    PRIMARY KEY (RoleID, PermissionID)
);

CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY,
    Username NVARCHAR(100) UNIQUE NOT NULL,
    Email NVARCHAR(255),
    ADObjectGUID UNIQUEIDENTIFIER, -- Active Directory link
    Enabled BIT DEFAULT 1,
    Created DATETIME2 DEFAULT GETUTCDATE(),
    LastLogin DATETIME2
);

CREATE TABLE UserRoles (
    UserID INT FOREIGN KEY REFERENCES Users(UserID),
    RoleID INT FOREIGN KEY REFERENCES Roles(RoleID),
    AssignedBy INT FOREIGN KEY REFERENCES Users(UserID),
    AssignedDate DATETIME2 DEFAULT GETUTCDATE(),
    PRIMARY KEY (UserID, RoleID)
);
```

**Standard Roles:**

| Role | Permissions | Use Case |
|------|-------------|----------|
| **Administrator** | Full system access | IT admins |
| **Content Manager** | Upload content, manage overlays, scheduling | Marketing team |
| **Operator** | View dashboards, restart clients, view logs | Operations team |
| **Viewer** | Read-only access | Executives, stakeholders |
| **Client** | Device-only access | Raspberry Pi devices |

**Permission Matrix:**

| Resource | Admin | Content Manager | Operator | Viewer | Client |
|----------|-------|----------------|----------|---------|--------|
| Content: Upload | ✓ | ✓ | ✗ | ✗ | ✗ |
| Content: View | ✓ | ✓ | ✓ | ✓ | ✗ |
| Content: Delete | ✓ | ✓ | ✗ | ✗ | ✗ |
| Overlay: Create | ✓ | ✓ | ✗ | ✗ | ✗ |
| Overlay: Edit | ✓ | ✓ | ✗ | ✗ | ✗ |
| Client: Register | ✓ | ✗ | ✗ | ✗ | ✓ |
| Client: View | ✓ | ✓ | ✓ | ✓ | ✗ |
| Client: Reboot | ✓ | ✗ | ✓ | ✗ | ✗ |
| Client: Configure | ✓ | ✗ | ✗ | ✗ | ✗ |
| User: Create | ✓ | ✗ | ✗ | ✗ | ✗ |
| User: Assign Roles | ✓ | ✗ | ✗ | ✗ | ✗ |
| Schedule: Create | ✓ | ✓ | ✗ | ✗ | ✗ |
| Schedule: View | ✓ | ✓ | ✓ | ✓ | ✗ |
| Audit Log: View | ✓ | ✗ | ✗ | ✗ | ✗ |
| System: Configure | ✓ | ✗ | ✗ | ✗ | ✗ |

### Active Directory Integration

**LDAP Configuration:**

```json
{
  "ldap": {
    "servers": [
      {
        "url": "ldap://dc1.company.local:389",
        "backupUrl": "ldap://dc2.company.local:389"
      }
    ],
    "baseDN": "DC=company,DC=local",
    "bindDN": "CN=MakerScreen Service,OU=Services,DC=company,DC=local",
    "bindPassword": "<encrypted>",
    "userSearchBase": "OU=Users,DC=company,DC=local",
    "userSearchFilter": "(&(objectClass=user)(sAMAccountName={0}))",
    "groupSearchBase": "OU=Groups,DC=company,DC=local",
    "groupSearchFilter": "(member={0})",
    "attributes": {
      "username": "sAMAccountName",
      "email": "mail",
      "displayName": "displayName",
      "objectGUID": "objectGUID",
      "memberOf": "memberOf"
    },
    "groupMapping": {
      "CN=MakerScreen Admins,OU=Groups,DC=company,DC=local": "Administrator",
      "CN=MakerScreen Content,OU=Groups,DC=company,DC=local": "Content Manager",
      "CN=MakerScreen Operators,OU=Groups,DC=company,DC=local": "Operator",
      "CN=MakerScreen Viewers,OU=Groups,DC=company,DC=local": "Viewer"
    },
    "sync": {
      "enabled": true,
      "intervalMinutes": 60,
      "removeDeletedUsers": false,
      "updateExistingUsers": true
    },
    "security": {
      "useTLS": true,
      "validateCertificate": true,
      "minimumTLSVersion": "1.2"
    }
  }
}
```

**Authentication Flow:**

```csharp
// C# AD Authentication
public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
{
    using var ldap = new LdapConnection(ldapServer);
    
    try
    {
        // 1. Attempt bind with user credentials
        ldap.Bind($"{username}@{domain}", password);
        
        // 2. Retrieve user details
        var searchRequest = new SearchRequest(
            baseDN,
            $"(sAMAccountName={username})",
            SearchScope.Subtree,
            "objectGUID", "memberOf", "mail", "displayName"
        );
        
        var response = (SearchResponse)ldap.SendRequest(searchRequest);
        if (response.Entries.Count == 0)
            return AuthenticationResult.UserNotFound;
        
        var entry = response.Entries[0];
        var objectGUID = new Guid((byte[])entry.Attributes["objectGUID"][0]);
        var groups = entry.Attributes["memberOf"];
        
        // 3. Map AD groups to roles
        var roles = MapGroupsToRoles(groups);
        
        // 4. Create or update user in database
        var user = await CreateOrUpdateUser(username, objectGUID, roles);
        
        // 5. Generate JWT token
        var token = GenerateJWT(user, roles);
        
        // 6. Log successful authentication
        await AuditLog(user.UserID, "AUTH_SUCCESS", $"User {username} logged in");
        
        return new AuthenticationResult
        {
            Success = true,
            Token = token,
            User = user,
            Roles = roles
        };
    }
    catch (LdapException ex)
    {
        // Log failed authentication
        await AuditLog(null, "AUTH_FAILURE", $"Failed login attempt for {username}: {ex.Message}");
        return AuthenticationResult.InvalidCredentials;
    }
}
```

### Session Management

**JWT Token Configuration:**

```json
{
  "jwt": {
    "issuer": "MakerScreen Server",
    "audience": "MakerScreen Clients",
    "secretKey": "<256-bit key from DPAPI>",
    "algorithm": "HS256",
    "accessTokenExpiry": 3600,
    "refreshTokenExpiry": 2592000,
    "clockSkew": 300,
    "validateLifetime": true,
    "validateIssuer": true,
    "validateAudience": true
  }
}
```

**Token Claims:**

```json
{
  "sub": "john.doe@company.local",
  "user_id": "12345",
  "roles": ["Content Manager"],
  "permissions": ["content:upload", "content:view", "overlay:create"],
  "iat": 1640000000,
  "exp": 1640003600,
  "iss": "MakerScreen Server",
  "aud": "MakerScreen Clients"
}
```

**Security Measures:**
- Short-lived access tokens (1 hour)
- Long-lived refresh tokens (30 days)
- Tokens invalidated on logout
- Token blacklist for revocation
- Automatic renewal before expiry
- Secure storage (HttpOnly cookies, iOS Keychain)

---

## Data Protection

### Encryption at Rest

**Database Encryption:**

```sql
-- Enable Transparent Data Encryption (TDE)
USE master;
GO

-- Create Database Master Key
CREATE MASTER KEY ENCRYPTION BY PASSWORD = '<strong_password>';
GO

-- Create certificate for TDE
CREATE CERTIFICATE TDE_Certificate WITH SUBJECT = 'MakerScreen TDE Certificate';
GO

-- Backup certificate (CRITICAL - store securely)
BACKUP CERTIFICATE TDE_Certificate
TO FILE = 'C:\Backup\TDE_Certificate.cer'
WITH PRIVATE KEY (
    FILE = 'C:\Backup\TDE_Certificate_Key.pvk',
    ENCRYPTION BY PASSWORD = '<backup_password>'
);
GO

-- Use database
USE MakerScreen;
GO

-- Create Database Encryption Key
CREATE DATABASE ENCRYPTION KEY
WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE TDE_Certificate;
GO

-- Enable encryption
ALTER DATABASE MakerScreen
SET ENCRYPTION ON;
GO

-- Verify encryption status
SELECT 
    DB_NAME(database_id) AS DatabaseName,
    encryption_state,
    encryption_state_desc = CASE encryption_state
        WHEN 0 THEN 'No encryption'
        WHEN 1 THEN 'Unencrypted'
        WHEN 2 THEN 'Encryption in progress'
        WHEN 3 THEN 'Encrypted'
        WHEN 4 THEN 'Key change in progress'
        WHEN 5 THEN 'Decryption in progress'
        WHEN 6 THEN 'Protection change in progress'
    END,
    percent_complete
FROM sys.dm_database_encryption_keys;
```

**File System Encryption (Windows):**

```powershell
# Enable BitLocker on data drive
Enable-BitLocker -MountPoint "D:" `
    -EncryptionMethod XtsAes256 `
    -UsedSpaceOnly `
    -TpmProtector

# Encrypt MakerScreen directories
$dirs = @(
    "C:\ProgramData\MakerScreen\Content",
    "C:\ProgramData\MakerScreen\Cache",
    "C:\ProgramData\MakerScreen\Logs"
)

foreach ($dir in $dirs) {
    cipher /e /s:$dir
}

# Verify encryption
cipher /u /n
```

**Raspberry Pi Encryption:**

```bash
# LUKS encryption for sensitive partitions
# (Applied during image build)

# Install cryptsetup
apt-get install -y cryptsetup

# Create encrypted volume
cryptsetup luksFormat --type luks2 --cipher aes-xts-plain64 \
    --key-size 512 --hash sha256 /dev/mmcblk0p3

# Open encrypted volume
cryptsetup luksOpen /dev/mmcblk0p3 encrypted_data

# Create filesystem
mkfs.ext4 /dev/mapper/encrypted_data

# Mount
mkdir -p /mnt/encrypted
mount /dev/mapper/encrypted_data /mnt/encrypted

# Add to /etc/crypttab for auto-unlock with key file
echo "encrypted_data /dev/mmcblk0p3 /etc/makerscreen/luks.key luks" >> /etc/crypttab

# Add to /etc/fstab
echo "/dev/mapper/encrypted_data /mnt/encrypted ext4 defaults 0 2" >> /etc/fstab
```

### Encryption in Transit

**TLS 1.3 Configuration:**

```csharp
// C# Server WebSocket Configuration
var webSocketOptions = new WebSocketOptions
{
    // Enable TLS 1.3 only
    SslProtocols = SslProtocols.Tls13,
    
    // Require client certificates
    ClientCertificateMode = ClientCertificateMode.RequireCertificate,
    
    // Custom certificate validation
    ClientCertificateValidation = (cert, chain, errors) =>
    {
        return ValidateClientCertificate(cert);
    },
    
    // Cipher suite configuration (strongest first)
    CipherSuitesPolicy = new CipherSuitesPolicy(new[]
    {
        TlsCipherSuite.TLS_AES_256_GCM_SHA384,
        TlsCipherSuite.TLS_AES_128_GCM_SHA256,
        TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256
    })
};

// Server certificate
var serverCert = new X509Certificate2(
    "server-cert.pfx",
    certPassword,
    X509KeyStorageFlags.MachineKeySet
);

webSocketOptions.ServerCertificate = serverCert;
```

**Perfect Forward Secrecy:**
- Ephemeral Diffie-Hellman (DHE) key exchange
- Elliptic Curve Diffie-Hellman (ECDHE)
- Session keys derived per connection
- No session key reuse

### Data Sanitization

**Secure Data Deletion:**

```csharp
// C# Secure deletion
public void SecureDelete(string filePath)
{
    if (!File.Exists(filePath))
        return;
    
    var fileInfo = new FileInfo(filePath);
    var fileLength = fileInfo.Length;
    
    // Overwrite file with random data (3 passes)
    using (var rng = RandomNumberGenerator.Create())
    {
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
        {
            byte[] buffer = new byte[4096];
            
            for (int pass = 0; pass < 3; pass++)
            {
                fs.Position = 0;
                
                for (long written = 0; written < fileLength; written += buffer.Length)
                {
                    rng.GetBytes(buffer);
                    int toWrite = (int)Math.Min(buffer.Length, fileLength - written);
                    fs.Write(buffer, 0, toWrite);
                }
                
                fs.Flush(true);
            }
        }
    }
    
    // Delete file
    File.Delete(filePath);
    
    // Verify deletion
    if (File.Exists(filePath))
        throw new IOException("File deletion failed");
}
```

**Database Record Sanitization:**

```sql
-- Stored procedure for secure user deletion
CREATE PROCEDURE sp_SecureDeleteUser
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    -- Audit the deletion
    INSERT INTO AuditLog (EventType, UserID, Description, Timestamp)
    VALUES ('USER_DELETED', @UserID, 
            (SELECT Username FROM Users WHERE UserID = @UserID), 
            GETUTCDATE());
    
    -- Remove from related tables
    DELETE FROM UserRoles WHERE UserID = @UserID;
    DELETE FROM UserSessions WHERE UserID = @UserID;
    
    -- Anonymize user data (for audit trail integrity)
    UPDATE Users
    SET Username = 'DELETED_' + CAST(UserID AS NVARCHAR),
        Email = NULL,
        ADObjectGUID = NULL,
        Enabled = 0
    WHERE UserID = @UserID;
    
    COMMIT TRANSACTION;
END;
```

---

## Audit & Compliance

### Comprehensive Audit Logging

**Audit Event Types:**

```csharp
public enum AuditEventType
{
    // Authentication
    AUTH_SUCCESS,
    AUTH_FAILURE,
    AUTH_LOGOUT,
    AUTH_TOKEN_REFRESH,
    
    // User Management
    USER_CREATED,
    USER_UPDATED,
    USER_DELETED,
    USER_ROLE_ASSIGNED,
    USER_ROLE_REVOKED,
    
    // Content Management
    CONTENT_UPLOADED,
    CONTENT_UPDATED,
    CONTENT_DELETED,
    CONTENT_DOWNLOADED,
    
    // Client Management
    CLIENT_REGISTERED,
    CLIENT_DEREGISTERED,
    CLIENT_REBOOTED,
    CLIENT_UPDATED,
    CLIENT_CONFIG_CHANGED,
    
    // Security Events
    CERTIFICATE_ISSUED,
    CERTIFICATE_REVOKED,
    SECURITY_VIOLATION,
    ACCESS_DENIED,
    
    // System Events
    SYSTEM_CONFIG_CHANGED,
    BACKUP_CREATED,
    BACKUP_RESTORED,
    SYSTEM_SHUTDOWN,
    SYSTEM_STARTUP
}
```

**Audit Log Schema:**

```sql
CREATE TABLE AuditLog (
    AuditID BIGINT PRIMARY KEY IDENTITY,
    Timestamp DATETIME2(7) DEFAULT SYSUTCDATETIME() NOT NULL,
    EventType NVARCHAR(50) NOT NULL,
    UserID INT FOREIGN KEY REFERENCES Users(UserID),
    IPAddress NVARCHAR(45), -- IPv6 compatible
    UserAgent NVARCHAR(500),
    Resource NVARCHAR(200),
    Action NVARCHAR(50),
    Result NVARCHAR(20), -- SUCCESS, FAILURE, DENIED
    Details NVARCHAR(MAX), -- JSON formatted
    SessionID UNIQUEIDENTIFIER,
    
    -- Immutability
    CONSTRAINT CK_AuditLog_NoUpdate CHECK (1=1), -- Enforced by trigger
    
    INDEX IX_AuditLog_Timestamp (Timestamp DESC),
    INDEX IX_AuditLog_UserID (UserID, Timestamp DESC),
    INDEX IX_AuditLog_EventType (EventType, Timestamp DESC)
);

-- Prevent updates and deletes
CREATE TRIGGER TR_AuditLog_PreventModification
ON AuditLog
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR('Audit log records cannot be modified or deleted', 16, 1);
    ROLLBACK TRANSACTION;
END;
```

**Audit Logging Implementation:**

```csharp
public class AuditLogger
{
    private readonly DbContext _context;
    private readonly IHttpContextAccessor _httpContext;
    
    public async Task LogAsync(
        AuditEventType eventType,
        string resource,
        string action,
        string result,
        object details = null)
    {
        var user = _httpContext.HttpContext?.User;
        var userId = user?.FindFirst("user_id")?.Value;
        var ipAddress = _httpContext.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = _httpContext.HttpContext?.Request?.Headers["User-Agent"].ToString();
        var sessionId = _httpContext.HttpContext?.Session?.Id;
        
        var auditEntry = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            EventType = eventType.ToString(),
            UserID = userId != null ? int.Parse(userId) : (int?)null,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            Resource = resource,
            Action = action,
            Result = result,
            Details = details != null ? JsonSerializer.Serialize(details) : null,
            SessionID = sessionId != null ? Guid.Parse(sessionId) : (Guid?)null
        };
        
        _context.AuditLog.Add(auditEntry);
        await _context.SaveChangesAsync();
        
        // Also log to file for redundancy
        await LogToFileAsync(auditEntry);
    }
    
    private async Task LogToFileAsync(AuditLog entry)
    {
        var logLine = $"{entry.Timestamp:O}|{entry.EventType}|{entry.UserID}|{entry.Resource}|{entry.Action}|{entry.Result}|{entry.Details}";
        await File.AppendAllTextAsync(
            $"C:\\ProgramData\\MakerScreen\\Logs\\audit_{DateTime.UtcNow:yyyyMMdd}.log",
            logLine + Environment.NewLine
        );
    }
}
```

### Compliance Features

**GDPR Compliance:**

1. **Data Subject Rights:**
   ```sql
   -- Right to Access
   CREATE PROCEDURE sp_GDPR_ExportUserData
       @UserID INT
   AS
   BEGIN
       -- Export all user data
       SELECT * FROM Users WHERE UserID = @UserID;
       SELECT * FROM UserRoles WHERE UserID = @UserID;
       SELECT * FROM AuditLog WHERE UserID = @UserID;
       -- etc.
   END;
   
   -- Right to Erasure ("Right to be Forgotten")
   CREATE PROCEDURE sp_GDPR_DeleteUserData
       @UserID INT
   AS
   BEGIN
       -- Anonymize or delete user data
       EXEC sp_SecureDeleteUser @UserID;
   END;
   ```

2. **Data Minimization:**
   - Collect only necessary data
   - Automatic data retention policies
   - Scheduled cleanup of old data

3. **Privacy by Design:**
   - Default to minimal data collection
   - Encryption by default
   - Access controls from day one

**Audit Trail Retention:**

```sql
-- Automatic archival of old audit logs
CREATE PROCEDURE sp_ArchiveOldAuditLogs
AS
BEGIN
    DECLARE @ArchiveDate DATETIME2 = DATEADD(YEAR, -1, GETUTCDATE());
    
    -- Move to archive table
    INSERT INTO AuditLogArchive
    SELECT * FROM AuditLog WHERE Timestamp < @ArchiveDate;
    
    -- Keep 90-day window in main table for performance
    DELETE FROM AuditLog WHERE Timestamp < DATEADD(DAY, -90, GETUTCDATE());
END;

-- Schedule via SQL Server Agent (daily at 2 AM)
```

---

## Security Operations

### Security Monitoring

**Real-Time Monitoring:**

```csharp
public class SecurityMonitor
{
    private readonly ILogger _logger;
    private readonly IAuditLogger _audit;
    
    // Failed authentication tracking
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _failedAttempts = new();
    
    public async Task<bool> CheckRateLimitAsync(string identifier)
    {
        var attempts = _failedAttempts.GetOrAdd(identifier, _ => new Queue<DateTime>());
        
        lock (attempts)
        {
            // Remove attempts older than 15 minutes
            while (attempts.Count > 0 && attempts.Peek() < DateTime.UtcNow.AddMinutes(-15))
                attempts.Dequeue();
            
            // Check if rate limit exceeded (5 attempts in 15 minutes)
            if (attempts.Count >= 5)
            {
                await _audit.LogAsync(
                    AuditEventType.SECURITY_VIOLATION,
                    "Authentication",
                    "Rate Limit Exceeded",
                    "BLOCKED",
                    new { Identifier = identifier, Attempts = attempts.Count }
                );
                
                return false;
            }
            
            attempts.Enqueue(DateTime.UtcNow);
            return true;
        }
    }
    
    public async Task MonitorAnomalies()
    {
        // Unusual login times
        var unusualLogins = await DetectUnusualLoginTimes();
        if (unusualLogins.Any())
            await AlertAdministrators("Unusual Login Times", unusualLogins);
        
        // Multiple failed authentications
        var bruteForceattempts = await DetectBruteForceAttempts();
        if (bruteForceAttempts.Any())
            await AlertAdministrators("Potential Brute Force Attack", bruteForceAttempts);
        
        // Unauthorized access attempts
        var unauthorizedAccess = await DetectUnauthorizedAccess();
        if (unauthorizedAccess.Any())
            await AlertAdministrators("Unauthorized Access Attempts", unauthorizedAccess);
        
        // Unusual data volumes
        var dataAnomalies = await DetectDataVolumeAnomalies();
        if (dataAnomalies.Any())
            await AlertAdministrators("Unusual Data Transfer", dataAnomalies);
    }
}
```

**Security Metrics Dashboard:**

- Failed authentication rate
- Certificate validation failures
- Rate limit violations
- Unusual access patterns
- Network anomalies
- System resource usage

### Vulnerability Management

**Dependency Scanning:**

```yaml
# GitHub Actions workflow for dependency scanning
name: Security Scan

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  schedule:
    - cron: '0 0 * * 0' # Weekly

jobs:
  security-scan:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Run Dependency Check (NuGet)
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: 'MakerScreen'
          path: '.'
          format: 'HTML'
          
      - name: Run Snyk Security Scan
        uses: snyk/actions/dotnet@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
        with:
          args: --severity-threshold=high
          
      - name: Upload results
        uses: actions/upload-artifact@v3
        with:
          name: security-reports
          path: |
            dependency-check-report.html
            snyk-report.json
```

**Patch Management:**

1. **Automated Updates:**
   - Windows Update enabled
   - NuGet package updates weekly
   - Python package updates weekly
   - iOS dependency updates via Dependabot

2. **Update Process:**
   ```
   Security Advisory Published →
   Assess Impact →
   Test in Development →
   Deploy to Staging →
   Validate →
   Deploy to Production →
   Monitor
   ```

3. **Emergency Patches:**
   - Critical patches within 24 hours
   - High severity within 7 days
   - Medium severity within 30 days

---

## Incident Response

### Incident Response Plan

**Response Team:**

| Role | Responsibilities | Contact |
|------|-----------------|---------|
| Incident Commander | Overall coordination | Primary on-call |
| Security Lead | Investigation, containment | Security team |
| Technical Lead | Technical remediation | Development team |
| Communications | Stakeholder updates | Management |

**Incident Severity Levels:**

| Level | Description | Response Time | Escalation |
|-------|-------------|---------------|------------|
| **P1 - Critical** | Data breach, system compromise | Immediate | CEO, CISO |
| **P2 - High** | Potential breach, service disruption | 1 hour | CTO, Security Manager |
| **P3 - Medium** | Security violation, limited impact | 4 hours | Security Team |
| **P4 - Low** | Policy violation, no impact | Next business day | Team Lead |

**Response Procedure:**

```
1. DETECTION
   └─ Alert triggered
   └─ Initial assessment
   └─ Classify severity
   
2. CONTAINMENT
   └─ Isolate affected systems
   └─ Revoke compromised credentials
   └─ Block malicious IPs
   └─ Preserve evidence
   
3. INVESTIGATION
   └─ Analyze logs
   └─ Determine root cause
   └─ Assess impact
   └─ Document findings
   
4. ERADICATION
   └─ Remove threat
   └─ Patch vulnerabilities
   └─ Update security controls
   
5. RECOVERY
   └─ Restore services
   └─ Verify security
   └─ Monitor for recurrence
   
6. POST-INCIDENT
   └─ Incident report
   └─ Lessons learned
   └─ Update procedures
   └─ Implement improvements
```

### Common Scenarios

**Scenario 1: Suspected Certificate Compromise**

```
DETECTION:
- Alert: Client certificate used from unexpected IP

IMMEDIATE ACTIONS:
1. Revoke affected certificate
2. Add to CRL
3. Force all clients to check CRL
4. Block unauthorized IP at firewall
5. Review audit logs for extent of access

INVESTIGATION:
1. Determine how certificate was compromised
2. Check for other compromised certificates
3. Review all access from affected client
4. Assess data exposure

REMEDIATION:
1. Issue new certificate to legitimate client
2. Tighten certificate storage permissions
3. Implement additional monitoring
4. Update incident response plan

RECOVERY:
1. Deploy new certificate
2. Verify client connectivity
3. Monitor for 48 hours
4. Document incident
```

**Scenario 2: Unauthorized Access Attempt**

```
DETECTION:
- Alert: Multiple failed authentication attempts

IMMEDIATE ACTIONS:
1. Block source IP temporarily
2. Review affected accounts
3. Force password reset if needed
4. Enable MFA if not already

INVESTIGATION:
1. Analyze attack pattern
2. Determine if credentials leaked
3. Check for lateral movement
4. Review logs for successful attempts

REMEDIATION:
1. Permanent IP block if malicious
2. Update rate limiting rules
3. Strengthen password policies
4. User security awareness training

RECOVERY:
1. Monitor for repeat attempts
2. Validate all account access
3. Document lessons learned
```

### Forensics & Evidence Collection

**Evidence Preservation:**

```powershell
# PowerShell script for evidence collection

$evidenceDir = "C:\Evidence\Incident_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $evidenceDir

# 1. Collect audit logs
Copy-Item "C:\ProgramData\MakerScreen\Logs\*" -Destination "$evidenceDir\Logs\" -Recurse

# 2. Export security event logs
wevtutil epl Security "$evidenceDir\Security.evtx"
wevtutil epl Application "$evidenceDir\Application.evtx"
wevtutil epl System "$evidenceDir\System.evtx"

# 3. Database backup
sqlcmd -Q "BACKUP DATABASE MakerScreen TO DISK='$evidenceDir\Database.bak'"

# 4. Network configuration
ipconfig /all > "$evidenceDir\network_config.txt"
netstat -ano > "$evidenceDir\network_connections.txt"

# 5. System information
systeminfo > "$evidenceDir\system_info.txt"
Get-Process | Export-Csv "$evidenceDir\processes.csv"

# 6. Hash all evidence files
Get-ChildItem $evidenceDir -Recurse -File | 
    ForEach-Object {
        "$($_.FullName): $(Get-FileHash $_.FullName -Algorithm SHA256).Hash"
    } > "$evidenceDir\hashes.txt"

# 7. Compress and protect
Compress-Archive -Path $evidenceDir -DestinationPath "$evidenceDir.zip"
```

---

## Security Best Practices Summary

### Development

- ✓ Use parameterized queries (prevent SQL injection)
- ✓ Validate all input (whitelist, not blacklist)
- ✓ Sanitize output (prevent XSS)
- ✓ Use secure random number generation
- ✓ Never hardcode secrets
- ✓ Implement proper error handling (no sensitive info in errors)
- ✓ Code review all security-critical code
- ✓ Static analysis on every commit
- ✓ Dependency vulnerability scanning

### Deployment

- ✓ Change default passwords immediately
- ✓ Disable unnecessary services
- ✓ Apply principle of least privilege
- ✓ Enable firewalls
- ✓ Use strong encryption everywhere
- ✓ Keep systems patched
- ✓ Regular backups (tested restores)
- ✓ Network segmentation
- ✓ Monitor and log everything

### Operations

- ✓ Regular security audits
- ✓ Penetration testing annually
- ✓ Security awareness training
- ✓ Incident response drills
- ✓ Review audit logs weekly
- ✓ Update certificates before expiry
- ✓ Test disaster recovery quarterly
- ✓ Maintain security documentation
- ✓ Stay informed of new threats

---

**Document Version**: 1.0  
**Last Updated**: 2024-01-15  
**Classification**: Internal  
**Owner**: Security Team
