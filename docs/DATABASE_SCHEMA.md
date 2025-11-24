# MakerScreen Database Schema

## Overview

The MakerScreen database is built on SQL Server 2019+ and utilizes advanced features including:
- **FILESTREAM** for efficient binary content storage
- **Temporal Tables** for complete change history
- **Full-Text Search** for content discovery
- **Transparent Data Encryption (TDE)** for security
- **Always Encrypted** for sensitive data
- **Columnstore Indexes** for analytics

---

## Entity Relationship Diagram

```
┌─────────────┐
│   Users     │──────┐
└─────────────┘      │
       │             │
       │             │
┌─────────────┐      │
│   Roles     │      │
└─────────────┘      │
       │             │
       │             │
┌─────────────┐      │
│ Permissions │      │
└─────────────┘      │
                     │
┌─────────────┐      │
│  Content    │◀─────┤
└─────────────┘      │
       │             │
       │             │
┌─────────────┐      │
│  Overlays   │◀─────┤
└─────────────┘      │
       │             │
       │             │
┌─────────────┐      │
│  Playlists  │◀─────┤
└─────────────┘      │
       │             │
       │             │
┌─────────────┐      │
│  Clients    │◀─────┤
└─────────────┘      │
       │             │
       │             │
┌─────────────┐      │
│  AuditLog   │◀─────┘
└─────────────┘
```

---

## Database Creation

```sql
-- Create Database with FILESTREAM support
CREATE DATABASE MakerScreen
ON PRIMARY
(
    NAME = MakerScreen_Data,
    FILENAME = 'D:\SQL\Data\MakerScreen_Data.mdf',
    SIZE = 1GB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 256MB
),
FILEGROUP FileStreamGroup CONTAINS FILESTREAM
(
    NAME = MakerScreen_FileStream,
    FILENAME = 'D:\SQL\FileStream\MakerScreen_FS'
)
LOG ON
(
    NAME = MakerScreen_Log,
    FILENAME = 'D:\SQL\Logs\MakerScreen_Log.ldf',
    SIZE = 512MB,
    MAXSIZE = 10GB,
    FILEGROWTH = 128MB
);
GO

-- Enable FILESTREAM
EXEC sp_configure 'filestream access level', 2;
RECONFIGURE;
GO

ALTER DATABASE MakerScreen
SET RECOVERY FULL;
GO

USE MakerScreen;
GO
```

---

## Core Tables

### Users & Authentication

```sql
-- Users Table
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) UNIQUE NOT NULL,
    Email NVARCHAR(255),
    DisplayName NVARCHAR(200),
    
    -- Active Directory Integration
    ADObjectGUID UNIQUEIDENTIFIER UNIQUE,
    ADDomainSID NVARCHAR(100),
    
    -- Local Authentication (if not using AD)
    PasswordHash VARBINARY(64), -- SHA-512
    PasswordSalt VARBINARY(32),
    
    -- Account Status
    Enabled BIT DEFAULT 1,
    Locked BIT DEFAULT 0,
    FailedLoginAttempts INT DEFAULT 0,
    LastFailedLogin DATETIME2(7),
    LastSuccessfulLogin DATETIME2(7),
    PasswordLastChanged DATETIME2(7),
    MustChangePassword BIT DEFAULT 0,
    
    -- Metadata
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    -- Soft Delete
    Deleted BIT DEFAULT 0,
    DeletedDate DATETIME2(7),
    DeletedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_Users_Username (Username),
    INDEX IX_Users_Email (Email),
    INDEX IX_Users_ADObjectGUID (ADObjectGUID),
    INDEX IX_Users_Enabled (Enabled) WHERE Enabled = 1
);
GO

-- Roles Table
CREATE TABLE Roles (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) UNIQUE NOT NULL,
    Description NVARCHAR(500),
    IsSystemRole BIT DEFAULT 0, -- Cannot be deleted
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    Modified DATETIME2(7),
    
    INDEX IX_Roles_RoleName (RoleName)
);
GO

-- Permissions Table
CREATE TABLE Permissions (
    PermissionID INT PRIMARY KEY IDENTITY(1,1),
    PermissionName NVARCHAR(100) UNIQUE NOT NULL,
    Resource NVARCHAR(100) NOT NULL,
    Action NVARCHAR(50) NOT NULL, -- CREATE, READ, UPDATE, DELETE, EXECUTE
    Description NVARCHAR(500),
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    
    INDEX IX_Permissions_Resource (Resource),
    INDEX IX_Permissions_Name (PermissionName)
);
GO

-- Role-Permission Mapping
CREATE TABLE RolePermissions (
    RoleID INT FOREIGN KEY REFERENCES Roles(RoleID) ON DELETE CASCADE,
    PermissionID INT FOREIGN KEY REFERENCES Permissions(PermissionID) ON DELETE CASCADE,
    GrantedDate DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    GrantedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    PRIMARY KEY (RoleID, PermissionID)
);
GO

-- User-Role Mapping
CREATE TABLE UserRoles (
    UserID INT FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    RoleID INT FOREIGN KEY REFERENCES Roles(RoleID) ON DELETE CASCADE,
    AssignedDate DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    AssignedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ExpiryDate DATETIME2(7), -- Optional role expiration
    
    PRIMARY KEY (UserID, RoleID)
);
GO

-- User Sessions
CREATE TABLE UserSessions (
    SessionID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserID INT FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    AccessToken NVARCHAR(MAX), -- JWT token (encrypted)
    RefreshToken NVARCHAR(MAX), -- Encrypted
    IPAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    DeviceType NVARCHAR(50), -- Windows, iOS, Web
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    LastActivity DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    ExpiresAt DATETIME2(7) NOT NULL,
    Revoked BIT DEFAULT 0,
    RevokedAt DATETIME2(7),
    RevokedReason NVARCHAR(200),
    
    INDEX IX_Sessions_UserID (UserID),
    INDEX IX_Sessions_ExpiresAt (ExpiresAt) WHERE Revoked = 0,
    INDEX IX_Sessions_Token (AccessToken(900)) -- Hash index
);
GO
```

### Content Management

```sql
-- Content Table with FILESTREAM
CREATE TABLE Content (
    ContentID INT PRIMARY KEY IDENTITY(1,1),
    ContentGUID UNIQUEIDENTIFIER ROWGUIDCOL UNIQUE DEFAULT NEWID(),
    
    -- Metadata
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Tags NVARCHAR(500), -- Comma-separated or JSON
    Category NVARCHAR(100),
    
    -- File Information
    FileName NVARCHAR(500) NOT NULL,
    FileExtension NVARCHAR(10),
    MimeType NVARCHAR(100),
    FileSize BIGINT,
    FileSHA256 VARBINARY(32), -- For deduplication
    
    -- Image Dimensions
    Width INT,
    Height INT,
    AspectRatio DECIMAL(10,4),
    
    -- FILESTREAM Storage
    FileData VARBINARY(MAX) FILESTREAM,
    ThumbnailData VARBINARY(MAX), -- Small preview (200x200)
    MediumThumbnail VARBINARY(MAX), -- Medium preview (800x600)
    
    -- Version Control
    Version INT DEFAULT 1,
    ParentContentID INT FOREIGN KEY REFERENCES Content(ContentID),
    IsLatestVersion BIT DEFAULT 1,
    
    -- Lifecycle
    Status NVARCHAR(20) DEFAULT 'Active', -- Active, Archived, Deleted
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    -- Soft Delete
    Deleted BIT DEFAULT 0,
    DeletedDate DATETIME2(7),
    DeletedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    -- Usage Tracking
    ViewCount BIGINT DEFAULT 0,
    LastViewed DATETIME2(7),
    DeploymentCount INT DEFAULT 0, -- How many clients showing this
    
    INDEX IX_Content_Title (Title),
    INDEX IX_Content_Tags (Tags),
    INDEX IX_Content_SHA256 (FileSHA256),
    INDEX IX_Content_Status (Status) WHERE Status = 'Active',
    INDEX IX_Content_Created (Created DESC)
);
GO

-- Enable Full-Text Search on Content
CREATE FULLTEXT CATALOG ContentCatalog AS DEFAULT;
CREATE FULLTEXT INDEX ON Content(Title, Description, Tags)
    KEY INDEX PK__Content__2C09FC11... -- Replace with actual PK name
    WITH STOPLIST = SYSTEM;
GO

-- Content History (Temporal Table alternative)
CREATE TABLE ContentHistory (
    HistoryID BIGINT PRIMARY KEY IDENTITY(1,1),
    ContentID INT FOREIGN KEY REFERENCES Content(ContentID),
    ChangeType NVARCHAR(20), -- INSERT, UPDATE, DELETE
    ChangedFields NVARCHAR(MAX), -- JSON
    OldValues NVARCHAR(MAX), -- JSON
    NewValues NVARCHAR(MAX), -- JSON
    ChangedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ChangedAt DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    
    INDEX IX_ContentHistory_ContentID (ContentID, ChangedAt DESC)
);
GO
```

### Overlay System

```sql
-- Data Sources
CREATE TABLE DataSources (
    DataSourceID INT PRIMARY KEY IDENTITY(1,1),
    DataSourceName NVARCHAR(100) UNIQUE NOT NULL,
    DataSourceType NVARCHAR(50) NOT NULL, -- SQL, REST, File, Static
    ConnectionString NVARCHAR(MAX), -- Encrypted
    
    -- SQL Server Specific
    DatabaseServer NVARCHAR(200),
    DatabaseName NVARCHAR(100),
    AuthType NVARCHAR(20), -- Windows, SQL, Certificate
    
    -- REST API Specific
    BaseURL NVARCHAR(500),
    AuthMethod NVARCHAR(50), -- Bearer, Basic, ApiKey
    ApiKey NVARCHAR(MAX), -- Encrypted
    
    -- Configuration
    RefreshIntervalSeconds INT DEFAULT 300,
    TimeoutSeconds INT DEFAULT 30,
    MaxRetries INT DEFAULT 3,
    
    -- Status
    Enabled BIT DEFAULT 1,
    LastSuccessfulConnection DATETIME2(7),
    LastError NVARCHAR(MAX),
    
    -- Metadata
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_DataSources_Name (DataSourceName),
    INDEX IX_DataSources_Type (DataSourceType)
);
GO

-- Data Queries
CREATE TABLE DataQueries (
    QueryID INT PRIMARY KEY IDENTITY(1,1),
    DataSourceID INT FOREIGN KEY REFERENCES DataSources(DataSourceID) ON DELETE CASCADE,
    QueryName NVARCHAR(100) NOT NULL,
    QueryType NVARCHAR(50), -- StoredProcedure, Query, REST
    
    -- Query Definition
    QueryText NVARCHAR(MAX), -- SQL or REST path
    Parameters NVARCHAR(MAX), -- JSON parameter definitions
    ResultMapping NVARCHAR(MAX), -- JSON field mapping
    
    -- Caching
    CacheDurationSeconds INT DEFAULT 300,
    LastExecuted DATETIME2(7),
    LastResult NVARCHAR(MAX), -- JSON cached result
    
    -- Metadata
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_DataQueries_DataSourceID (DataSourceID),
    INDEX IX_DataQueries_Name (QueryName)
);
GO

-- Overlay Templates
CREATE TABLE OverlayTemplates (
    TemplateID INT PRIMARY KEY IDENTITY(1,1),
    TemplateName NVARCHAR(100) UNIQUE NOT NULL,
    Description NVARCHAR(500),
    Category NVARCHAR(100),
    
    -- Template Definition (JSON)
    TemplateDefinition NVARCHAR(MAX), -- Complete overlay configuration
    PreviewImage VARBINARY(MAX),
    
    -- Usage
    IsPublic BIT DEFAULT 1,
    UsageCount INT DEFAULT 0,
    
    -- Metadata
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_OverlayTemplates_Name (TemplateName),
    INDEX IX_OverlayTemplates_Category (Category)
);
GO

-- Overlays
CREATE TABLE Overlays (
    OverlayID INT PRIMARY KEY IDENTITY(1,1),
    ContentID INT FOREIGN KEY REFERENCES Content(ContentID) ON DELETE CASCADE,
    OverlayName NVARCHAR(100) NOT NULL,
    OverlayType NVARCHAR(50) NOT NULL, -- Text, Ticker, Chart, QRCode, DateTime
    LayerIndex INT DEFAULT 0, -- Z-order (0 = bottom)
    
    -- Position & Size
    X INT NOT NULL,
    Y INT NOT NULL,
    Width INT NOT NULL,
    Height INT NOT NULL,
    
    -- Data Binding
    QueryID INT FOREIGN KEY REFERENCES DataQueries(QueryID) ON DELETE SET NULL,
    DataField NVARCHAR(100), -- Field from query result
    StaticData NVARCHAR(MAX), -- For static overlays
    
    -- Styling (JSON)
    Style NVARCHAR(MAX), -- Font, Color, Border, etc.
    
    -- Animation
    AnimationType NVARCHAR(50), -- None, Fade, Slide, Bounce
    AnimationDuration INT, -- Milliseconds
    
    -- Conditions
    DisplayConditions NVARCHAR(MAX), -- JSON rules for when to show
    
    -- Status
    Enabled BIT DEFAULT 1,
    
    -- Metadata
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_Overlays_ContentID (ContentID),
    INDEX IX_Overlays_LayerIndex (LayerIndex),
    INDEX IX_Overlays_QueryID (QueryID)
);
GO
```

### Clients & Devices

```sql
-- Clients
CREATE TABLE Clients (
    ClientID INT PRIMARY KEY IDENTITY(1,1),
    ClientGUID UNIQUEIDENTIFIER UNIQUE DEFAULT NEWID(),
    
    -- Identification
    Hostname NVARCHAR(100) NOT NULL,
    MACAddress NVARCHAR(17) UNIQUE NOT NULL, -- aa:bb:cc:dd:ee:ff
    IPAddress NVARCHAR(45), -- IPv6 compatible
    
    -- Hardware
    Model NVARCHAR(100), -- Raspberry Pi 4 Model B, etc.
    SerialNumber NVARCHAR(100),
    CPUCores INT,
    RAMSize BIGINT, -- Bytes
    StorageSize BIGINT, -- Bytes
    DisplayCount INT DEFAULT 1,
    
    -- Software
    OSVersion NVARCHAR(100),
    ClientVersion NVARCHAR(50), -- MakerScreen client version
    PythonVersion NVARCHAR(20),
    
    -- Display Configuration
    DisplayResolution NVARCHAR(20), -- 1920x1080
    DisplayOrientation INT DEFAULT 0, -- 0, 90, 180, 270
    DisplayRefreshRate INT DEFAULT 60,
    
    -- Location
    LocationName NVARCHAR(200),
    Building NVARCHAR(100),
    Floor NVARCHAR(50),
    Room NVARCHAR(50),
    PhysicalLocation NVARCHAR(500), -- Detailed description
    GPSLatitude DECIMAL(10,8),
    GPSLongitude DECIMAL(11,8),
    
    -- Status
    Status NVARCHAR(20) DEFAULT 'Online', -- Online, Offline, Error, Updating
    LastHeartbeat DATETIME2(7),
    LastIPAddress NVARCHAR(45),
    LastSeen DATETIME2(7),
    
    -- Security
    CertificateThumbprint NVARCHAR(64),
    CertificateExpiry DATETIME2(7),
    RegistrationToken UNIQUEIDENTIFIER,
    Approved BIT DEFAULT 0,
    ApprovedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ApprovedDate DATETIME2(7),
    
    -- Current State
    CurrentContentID INT FOREIGN KEY REFERENCES Content(ContentID),
    CurrentPlaylistID INT FOREIGN KEY REFERENCES Playlists(PlaylistID),
    
    -- Performance
    CPUUsage DECIMAL(5,2), -- Percentage
    RAMUsage BIGINT, -- Bytes
    CPUTemperature DECIMAL(5,2), -- Celsius
    Uptime BIGINT, -- Seconds
    
    -- Metadata
    Registered DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    RegisteredBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    -- Soft Delete
    Deleted BIT DEFAULT 0,
    DeletedDate DATETIME2(7),
    DeletedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_Clients_MACAddress (MACAddress),
    INDEX IX_Clients_Hostname (Hostname),
    INDEX IX_Clients_Status (Status),
    INDEX IX_Clients_Location (LocationName, Building, Floor),
    INDEX IX_Clients_LastHeartbeat (LastHeartbeat DESC)
);
GO

-- Client Groups
CREATE TABLE ClientGroups (
    GroupID INT PRIMARY KEY IDENTITY(1,1),
    GroupName NVARCHAR(100) UNIQUE NOT NULL,
    ParentGroupID INT FOREIGN KEY REFERENCES ClientGroups(GroupID),
    Description NVARCHAR(500),
    GroupType NVARCHAR(50), -- Location, Department, Custom
    
    -- Metadata
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_ClientGroups_Name (GroupName),
    INDEX IX_ClientGroups_Parent (ParentGroupID)
);
GO

-- Client-Group Membership
CREATE TABLE ClientGroupMembership (
    ClientID INT FOREIGN KEY REFERENCES Clients(ClientID) ON DELETE CASCADE,
    GroupID INT FOREIGN KEY REFERENCES ClientGroups(GroupID) ON DELETE CASCADE,
    AddedDate DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    AddedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    PRIMARY KEY (ClientID, GroupID)
);
GO

-- Client Health Metrics (Time Series Data)
CREATE TABLE ClientMetrics (
    MetricID BIGINT PRIMARY KEY IDENTITY(1,1),
    ClientID INT FOREIGN KEY REFERENCES Clients(ClientID) ON DELETE CASCADE,
    Timestamp DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    
    -- Performance Metrics
    CPUUsage DECIMAL(5,2),
    RAMUsage BIGINT,
    RAMTotal BIGINT,
    CPUTemperature DECIMAL(5,2),
    GPUTemperature DECIMAL(5,2),
    
    -- Network Metrics
    NetworkBytesReceived BIGINT,
    NetworkBytesSent BIGINT,
    NetworkErrors INT,
    Latency INT, -- Milliseconds to server
    
    -- Display Metrics
    CurrentFPS INT,
    DroppedFrames INT,
    
    -- Storage
    DiskUsage BIGINT,
    DiskTotal BIGINT,
    CacheSize BIGINT,
    
    INDEX IX_ClientMetrics_ClientTimestamp (ClientID, Timestamp DESC)
);
GO

-- Create Columnstore Index for Analytics
CREATE NONCLUSTERED COLUMNSTORE INDEX IX_ClientMetrics_Columnstore
ON ClientMetrics (ClientID, Timestamp, CPUUsage, RAMUsage, CPUTemperature);
GO
```

### Playlists & Scheduling

```sql
-- Playlists
CREATE TABLE Playlists (
    PlaylistID INT PRIMARY KEY IDENTITY(1,1),
    PlaylistName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    
    -- Playback Settings
    LoopPlaylist BIT DEFAULT 1,
    RandomOrder BIT DEFAULT 0,
    TransitionType NVARCHAR(50) DEFAULT 'Fade', -- Fade, Slide, Cut
    TransitionDuration INT DEFAULT 1000, -- Milliseconds
    
    -- Default Duration (if not specified per item)
    DefaultItemDuration INT DEFAULT 30, -- Seconds
    
    -- Status
    Enabled BIT DEFAULT 1,
    
    -- Metadata
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    -- Soft Delete
    Deleted BIT DEFAULT 0,
    DeletedDate DATETIME2(7),
    DeletedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_Playlists_Name (PlaylistName),
    INDEX IX_Playlists_Enabled (Enabled) WHERE Enabled = 1
);
GO

-- Playlist Items
CREATE TABLE PlaylistItems (
    PlaylistItemID INT PRIMARY KEY IDENTITY(1,1),
    PlaylistID INT FOREIGN KEY REFERENCES Playlists(PlaylistID) ON DELETE CASCADE,
    ContentID INT FOREIGN KEY REFERENCES Content(ContentID),
    
    -- Ordering
    DisplayOrder INT NOT NULL,
    
    -- Duration Override
    Duration INT, -- Seconds, NULL = use playlist default
    
    -- Scheduling within Playlist
    StartTime TIME, -- Show only after this time
    EndTime TIME, -- Show only before this time
    DaysOfWeek NVARCHAR(50), -- JSON array or comma-separated
    
    -- Status
    Enabled BIT DEFAULT 1,
    
    -- Metadata
    Added DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    AddedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_PlaylistItems_PlaylistID (PlaylistID, DisplayOrder),
    INDEX IX_PlaylistItems_ContentID (ContentID)
);
GO

-- Schedules
CREATE TABLE Schedules (
    ScheduleID INT PRIMARY KEY IDENTITY(1,1),
    ScheduleName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    
    -- Target
    PlaylistID INT FOREIGN KEY REFERENCES Playlists(PlaylistID),
    ContentID INT FOREIGN KEY REFERENCES Content(ContentID), -- Single content
    
    -- Time-based Schedule
    StartDateTime DATETIME2(7),
    EndDateTime DATETIME2(7),
    
    -- Recurrence (JSON)
    RecurrenceRule NVARCHAR(MAX), -- iCal RRULE format or JSON
    
    -- Day/Time Constraints
    DaysOfWeek NVARCHAR(50), -- JSON array
    TimeStart TIME,
    TimeEnd TIME,
    
    -- Priority (higher = more important)
    Priority INT DEFAULT 0,
    
    -- Conditional Rules (JSON)
    Conditions NVARCHAR(MAX), -- Weather, occupancy, custom SQL
    
    -- Status
    Enabled BIT DEFAULT 1,
    
    -- Metadata
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    -- Soft Delete
    Deleted BIT DEFAULT 0,
    DeletedDate DATETIME2(7),
    DeletedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_Schedules_Name (ScheduleName),
    INDEX IX_Schedules_DateTime (StartDateTime, EndDateTime),
    INDEX IX_Schedules_Priority (Priority DESC)
);
GO

-- Schedule Assignments (to Clients/Groups)
CREATE TABLE ScheduleAssignments (
    AssignmentID INT PRIMARY KEY IDENTITY(1,1),
    ScheduleID INT FOREIGN KEY REFERENCES Schedules(ScheduleID) ON DELETE CASCADE,
    
    -- Target (one of these)
    ClientID INT FOREIGN KEY REFERENCES Clients(ClientID),
    GroupID INT FOREIGN KEY REFERENCES ClientGroups(GroupID),
    
    -- Metadata
    Assigned DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    AssignedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    CHECK (
        (ClientID IS NOT NULL AND GroupID IS NULL) OR
        (ClientID IS NULL AND GroupID IS NOT NULL)
    ),
    
    INDEX IX_ScheduleAssignments_ScheduleID (ScheduleID),
    INDEX IX_ScheduleAssignments_ClientID (ClientID),
    INDEX IX_ScheduleAssignments_GroupID (GroupID)
);
GO
```

### Audit & Logging

```sql
-- Comprehensive Audit Log
CREATE TABLE AuditLog (
    AuditID BIGINT PRIMARY KEY IDENTITY(1,1),
    Timestamp DATETIME2(7) DEFAULT SYSUTCDATETIME() NOT NULL,
    
    -- Event Information
    EventType NVARCHAR(50) NOT NULL,
    EventCategory NVARCHAR(50), -- Authentication, Content, Client, System
    Severity NVARCHAR(20) DEFAULT 'Info', -- Debug, Info, Warning, Error, Critical
    
    -- User Context
    UserID INT FOREIGN KEY REFERENCES Users(UserID),
    Username NVARCHAR(100),
    IPAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    SessionID UNIQUEIDENTIFIER,
    
    -- Resource Information
    Resource NVARCHAR(200),
    ResourceID NVARCHAR(100),
    Action NVARCHAR(50),
    Result NVARCHAR(20), -- SUCCESS, FAILURE, DENIED
    
    -- Details
    Details NVARCHAR(MAX), -- JSON formatted details
    ErrorMessage NVARCHAR(MAX),
    StackTrace NVARCHAR(MAX),
    
    -- Request Information
    RequestPath NVARCHAR(500),
    RequestMethod NVARCHAR(10),
    RequestBody NVARCHAR(MAX),
    ResponseCode INT,
    
    -- Performance
    DurationMs INT,
    
    INDEX IX_AuditLog_Timestamp (Timestamp DESC),
    INDEX IX_AuditLog_UserID (UserID, Timestamp DESC),
    INDEX IX_AuditLog_EventType (EventType, Timestamp DESC),
    INDEX IX_AuditLog_Resource (Resource, ResourceID),
    INDEX IX_AuditLog_Severity (Severity) WHERE Severity IN ('Error', 'Critical')
);
GO

-- Prevent modifications to audit log
CREATE TRIGGER TR_AuditLog_PreventModification
ON AuditLog
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR('Audit log records cannot be modified or deleted', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO

-- Audit Log Archive (for old records)
CREATE TABLE AuditLogArchive (
    AuditID BIGINT PRIMARY KEY,
    Timestamp DATETIME2(7) NOT NULL,
    EventType NVARCHAR(50) NOT NULL,
    EventCategory NVARCHAR(50),
    Severity NVARCHAR(20),
    UserID INT,
    Username NVARCHAR(100),
    IPAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    SessionID UNIQUEIDENTIFIER,
    Resource NVARCHAR(200),
    ResourceID NVARCHAR(100),
    Action NVARCHAR(50),
    Result NVARCHAR(20),
    Details NVARCHAR(MAX),
    ErrorMessage NVARCHAR(MAX),
    StackTrace NVARCHAR(MAX),
    RequestPath NVARCHAR(500),
    RequestMethod NVARCHAR(10),
    RequestBody NVARCHAR(MAX),
    ResponseCode INT,
    DurationMs INT,
    ArchivedDate DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    
    INDEX IX_AuditLogArchive_Timestamp (Timestamp DESC)
);
GO

-- System Events
CREATE TABLE SystemEvents (
    EventID BIGINT PRIMARY KEY IDENTITY(1,1),
    Timestamp DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    EventType NVARCHAR(50) NOT NULL,
    Source NVARCHAR(100), -- Component that generated event
    Message NVARCHAR(MAX),
    Data NVARCHAR(MAX), -- JSON
    Acknowledged BIT DEFAULT 0,
    AcknowledgedBy INT FOREIGN KEY REFERENCES Users(UserID),
    AcknowledgedAt DATETIME2(7),
    
    INDEX IX_SystemEvents_Timestamp (Timestamp DESC),
    INDEX IX_SystemEvents_Type (EventType),
    INDEX IX_SystemEvents_Acknowledged (Acknowledged) WHERE Acknowledged = 0
);
GO
```

### Analytics & Reporting

```sql
-- Content Performance
CREATE TABLE ContentAnalytics (
    AnalyticsID BIGINT PRIMARY KEY IDENTITY(1,1),
    ContentID INT FOREIGN KEY REFERENCES Content(ContentID) ON DELETE CASCADE,
    ClientID INT FOREIGN KEY REFERENCES Clients(ClientID) ON DELETE CASCADE,
    
    -- Display Information
    DisplayStart DATETIME2(7) NOT NULL,
    DisplayEnd DATETIME2(7),
    Duration INT, -- Seconds
    
    -- Viewer Engagement (if sensors available)
    ViewerCount INT,
    AverageViewTime INT, -- Seconds
    
    -- Metadata
    Recorded DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    
    INDEX IX_ContentAnalytics_ContentID (ContentID, DisplayStart DESC),
    INDEX IX_ContentAnalytics_ClientID (ClientID, DisplayStart DESC),
    INDEX IX_ContentAnalytics_DisplayStart (DisplayStart DESC)
);
GO

-- Create Columnstore Index for Analytics Queries
CREATE NONCLUSTERED COLUMNSTORE INDEX IX_ContentAnalytics_Columnstore
ON ContentAnalytics (ContentID, ClientID, DisplayStart, Duration);
GO

-- Alert Rules
CREATE TABLE AlertRules (
    RuleID INT PRIMARY KEY IDENTITY(1,1),
    RuleName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    
    -- Condition (JSON or SQL)
    Condition NVARCHAR(MAX), -- Evaluated condition
    
    -- Threshold
    MetricName NVARCHAR(100),
    Operator NVARCHAR(10), -- >, <, =, >=, <=, !=
    ThresholdValue DECIMAL(18,4),
    
    -- Action
    AlertType NVARCHAR(50), -- Email, SMS, Push, Webhook
    Recipients NVARCHAR(MAX), -- JSON array
    MessageTemplate NVARCHAR(MAX),
    
    -- Rate Limiting
    CooldownMinutes INT DEFAULT 60, -- Don't alert again for X minutes
    LastTriggered DATETIME2(7),
    
    -- Status
    Enabled BIT DEFAULT 1,
    
    -- Metadata
    Created DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Modified DATETIME2(7),
    ModifiedBy INT FOREIGN KEY REFERENCES Users(UserID),
    
    INDEX IX_AlertRules_Enabled (Enabled) WHERE Enabled = 1
);
GO

-- Alert History
CREATE TABLE AlertHistory (
    AlertID BIGINT PRIMARY KEY IDENTITY(1,1),
    RuleID INT FOREIGN KEY REFERENCES AlertRules(RuleID),
    Triggered DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    MetricValue DECIMAL(18,4),
    Message NVARCHAR(MAX),
    Recipients NVARCHAR(MAX), -- Who was notified
    Status NVARCHAR(20), -- Sent, Failed, Acknowledged
    AcknowledgedBy INT FOREIGN KEY REFERENCES Users(UserID),
    AcknowledgedAt DATETIME2(7),
    
    INDEX IX_AlertHistory_RuleID (RuleID, Triggered DESC),
    INDEX IX_AlertHistory_Triggered (Triggered DESC),
    INDEX IX_AlertHistory_Status (Status) WHERE Status = 'Sent'
);
GO
```

---

## Stored Procedures

### User Management

```sql
-- Create User
CREATE PROCEDURE sp_CreateUser
    @Username NVARCHAR(100),
    @Email NVARCHAR(255),
    @DisplayName NVARCHAR(200),
    @ADObjectGUID UNIQUEIDENTIFIER = NULL,
    @CreatedBy INT,
    @NewUserID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        INSERT INTO Users (Username, Email, DisplayName, ADObjectGUID, CreatedBy)
        VALUES (@Username, @Email, @DisplayName, @ADObjectGUID, @CreatedBy);
        
        SET @NewUserID = SCOPE_IDENTITY();
        
        -- Audit
        INSERT INTO AuditLog (EventType, UserID, Resource, Action, Result, Details)
        VALUES ('USER_CREATED', @CreatedBy, 'User', 'CREATE', 'SUCCESS',
                JSON_OBJECT('NewUserID', @NewUserID, 'Username', @Username));
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Assign Role to User
CREATE PROCEDURE sp_AssignRole
    @UserID INT,
    @RoleID INT,
    @AssignedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserID = @UserID AND RoleID = @RoleID)
        BEGIN
            INSERT INTO UserRoles (UserID, RoleID, AssignedBy)
            VALUES (@UserID, @RoleID, @AssignedBy);
            
            -- Audit
            INSERT INTO AuditLog (EventType, UserID, Resource, Action, Result, Details)
            VALUES ('USER_ROLE_ASSIGNED', @AssignedBy, 'UserRole', 'CREATE', 'SUCCESS',
                    JSON_OBJECT('TargetUserID', @UserID, 'RoleID', @RoleID));
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
```

### Content Management

```sql
-- Upload Content
CREATE PROCEDURE sp_UploadContent
    @Title NVARCHAR(200),
    @FileName NVARCHAR(500),
    @FileExtension NVARCHAR(10),
    @FileSize BIGINT,
    @FileSHA256 VARBINARY(32),
    @Width INT,
    @Height INT,
    @CreatedBy INT,
    @ContentID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check for duplicate (same SHA256)
        IF EXISTS (SELECT 1 FROM Content WHERE FileSHA256 = @FileSHA256 AND Deleted = 0)
        BEGIN
            -- Return existing content
            SELECT @ContentID = ContentID 
            FROM Content 
            WHERE FileSHA256 = @FileSHA256 AND Deleted = 0;
            
            -- Audit
            INSERT INTO AuditLog (EventType, UserID, Resource, Action, Result, Details)
            VALUES ('CONTENT_DUPLICATE_DETECTED', @CreatedBy, 'Content', 'UPLOAD', 'SUCCESS',
                    JSON_OBJECT('ContentID', @ContentID, 'SHA256', CONVERT(NVARCHAR(64), @FileSHA256, 2)));
        END
        ELSE
        BEGIN
            -- Create new content record
            INSERT INTO Content (Title, FileName, FileExtension, FileSize, FileSHA256, 
                               Width, Height, AspectRatio, CreatedBy)
            VALUES (@Title, @FileName, @FileExtension, @FileSize, @FileSHA256,
                   @Width, @Height, CAST(@Width AS DECIMAL(10,4)) / @Height, @CreatedBy);
            
            SET @ContentID = SCOPE_IDENTITY();
            
            -- Audit
            INSERT INTO AuditLog (EventType, UserID, Resource, Action, Result, Details)
            VALUES ('CONTENT_UPLOADED', @CreatedBy, 'Content', 'UPLOAD', 'SUCCESS',
                    JSON_OBJECT('ContentID', @ContentID, 'Title', @Title, 'FileSize', @FileSize));
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
```

### Client Management

```sql
-- Register Client
CREATE PROCEDURE sp_RegisterClient
    @Hostname NVARCHAR(100),
    @MACAddress NVARCHAR(17),
    @IPAddress NVARCHAR(45),
    @Model NVARCHAR(100),
    @OSVersion NVARCHAR(100),
    @ClientVersion NVARCHAR(50),
    @AutoApprove BIT = 0,
    @RegisteredBy INT = NULL,
    @ClientID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if client already exists
        IF EXISTS (SELECT 1 FROM Clients WHERE MACAddress = @MACAddress)
        BEGIN
            -- Update existing client
            UPDATE Clients
            SET Hostname = @Hostname,
                IPAddress = @IPAddress,
                LastSeen = SYSUTCDATETIME(),
                Status = 'Online',
                OSVersion = @OSVersion,
                ClientVersion = @ClientVersion
            WHERE MACAddress = @MACAddress;
            
            SELECT @ClientID = ClientID FROM Clients WHERE MACAddress = @MACAddress;
            
            -- Audit
            INSERT INTO AuditLog (EventType, Resource, Action, Result, Details)
            VALUES ('CLIENT_RECONNECTED', 'Client', 'UPDATE', 'SUCCESS',
                    JSON_OBJECT('ClientID', @ClientID, 'Hostname', @Hostname, 'IP', @IPAddress));
        END
        ELSE
        BEGIN
            -- Create new client
            INSERT INTO Clients (Hostname, MACAddress, IPAddress, Model, OSVersion, 
                               ClientVersion, Approved, RegisteredBy, LastSeen, Status)
            VALUES (@Hostname, @MACAddress, @IPAddress, @Model, @OSVersion,
                   @ClientVersion, @AutoApprove, @RegisteredBy, SYSUTCDATETIME(), 'Online');
            
            SET @ClientID = SCOPE_IDENTITY();
            
            -- Generate registration token
            UPDATE Clients
            SET RegistrationToken = NEWID()
            WHERE ClientID = @ClientID;
            
            -- Audit
            INSERT INTO AuditLog (EventType, Resource, Action, Result, Details)
            VALUES ('CLIENT_REGISTERED', 'Client', 'CREATE', 'SUCCESS',
                    JSON_OBJECT('ClientID', @ClientID, 'Hostname', @Hostname, 
                               'MAC', @MACAddress, 'AutoApproved', @AutoApprove));
            
            -- Alert if not auto-approved
            IF @AutoApprove = 0
            BEGIN
                INSERT INTO SystemEvents (EventType, Source, Message, Data)
                VALUES ('NEW_CLIENT_PENDING', 'ClientRegistration',
                       'New client pending approval: ' + @Hostname,
                       JSON_OBJECT('ClientID', @ClientID, 'Hostname', @Hostname, 'MAC', @MACAddress));
            END
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Update Client Heartbeat
CREATE PROCEDURE sp_UpdateClientHeartbeat
    @ClientID INT,
    @IPAddress NVARCHAR(45) = NULL,
    @CPUUsage DECIMAL(5,2) = NULL,
    @RAMUsage BIGINT = NULL,
    @CPUTemperature DECIMAL(5,2) = NULL,
    @CurrentContentID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Clients
    SET LastHeartbeat = SYSUTCDATETIME(),
        LastSeen = SYSUTCDATETIME(),
        Status = 'Online',
        LastIPAddress = COALESCE(@IPAddress, LastIPAddress),
        CPUUsage = COALESCE(@CPUUsage, CPUUsage),
        RAMUsage = COALESCE(@RAMUsage, RAMUsage),
        CPUTemperature = COALESCE(@CPUTemperature, CPUTemperature),
        CurrentContentID = COALESCE(@CurrentContentID, CurrentContentID)
    WHERE ClientID = @ClientID;
    
    -- Record metrics
    IF @CPUUsage IS NOT NULL OR @RAMUsage IS NOT NULL OR @CPUTemperature IS NOT NULL
    BEGIN
        INSERT INTO ClientMetrics (ClientID, CPUUsage, RAMUsage, CPUTemperature)
        VALUES (@ClientID, @CPUUsage, @RAMUsage, @CPUTemperature);
    END
END;
GO
```

---

## Views

### Active Users View
```sql
CREATE VIEW vw_ActiveUsers
AS
SELECT 
    u.UserID,
    u.Username,
    u.Email,
    u.DisplayName,
    STRING_AGG(r.RoleName, ', ') AS Roles,
    u.LastSuccessfulLogin,
    u.Created
FROM Users u
LEFT JOIN UserRoles ur ON u.UserID = ur.UserID
LEFT JOIN Roles r ON ur.RoleID = r.RoleID
WHERE u.Enabled = 1 AND u.Deleted = 0
GROUP BY u.UserID, u.Username, u.Email, u.DisplayName, u.LastSuccessfulLogin, u.Created;
GO
```

### Client Status Dashboard
```sql
CREATE VIEW vw_ClientStatusDashboard
AS
SELECT 
    c.ClientID,
    c.Hostname,
    c.MACAddress,
    c.IPAddress,
    c.LocationName,
    c.Building,
    c.Floor,
    c.Status,
    c.LastHeartbeat,
    DATEDIFF(SECOND, c.LastHeartbeat, SYSUTCDATETIME()) AS SecondsSinceHeartbeat,
    CASE 
        WHEN DATEDIFF(SECOND, c.LastHeartbeat, SYSUTCDATETIME()) > 300 THEN 'Offline'
        WHEN c.Status = 'Error' THEN 'Error'
        ELSE 'Online'
    END AS EffectiveStatus,
    c.CPUUsage,
    c.RAMUsage,
    c.CPUTemperature,
    cnt.Title AS CurrentContent,
    p.PlaylistName AS CurrentPlaylist
FROM Clients c
LEFT JOIN Content cnt ON c.CurrentContentID = cnt.ContentID
LEFT JOIN Playlists p ON c.CurrentPlaylistID = p.PlaylistID
WHERE c.Deleted = 0;
GO
```

### Content Usage Statistics
```sql
CREATE VIEW vw_ContentUsageStats
AS
SELECT 
    c.ContentID,
    c.Title,
    c.FileName,
    c.FileSize,
    c.Created,
    c.CreatedBy,
    u.Username AS CreatedByUsername,
    c.ViewCount,
    c.DeploymentCount,
    COUNT(DISTINCT ca.ClientID) AS UniqueClients,
    SUM(ca.Duration) AS TotalDisplaySeconds,
    AVG(ca.Duration) AS AverageDisplaySeconds,
    MAX(ca.DisplayStart) AS LastDisplayed
FROM Content c
LEFT JOIN Users u ON c.CreatedBy = u.UserID
LEFT JOIN ContentAnalytics ca ON c.ContentID = ca.ContentID
WHERE c.Deleted = 0
GROUP BY c.ContentID, c.Title, c.FileName, c.FileSize, c.Created, 
         c.CreatedBy, u.Username, c.ViewCount, c.DeploymentCount;
GO
```

---

## Functions

### Check User Permission
```sql
CREATE FUNCTION fn_UserHasPermission(
    @UserID INT,
    @Resource NVARCHAR(100),
    @Action NVARCHAR(50)
)
RETURNS BIT
AS
BEGIN
    DECLARE @HasPermission BIT = 0;
    
    IF EXISTS (
        SELECT 1
        FROM UserRoles ur
        JOIN RolePermissions rp ON ur.RoleID = rp.RoleID
        JOIN Permissions p ON rp.PermissionID = p.PermissionID
        WHERE ur.UserID = @UserID
          AND p.Resource = @Resource
          AND p.Action = @Action
    )
    BEGIN
        SET @HasPermission = 1;
    END
    
    RETURN @HasPermission;
END;
GO
```

### Get Active Schedule for Client
```sql
CREATE FUNCTION fn_GetActiveSchedule(
    @ClientID INT,
    @CurrentDateTime DATETIME2(7)
)
RETURNS TABLE
AS
RETURN
(
    SELECT TOP 1
        s.ScheduleID,
        s.ScheduleName,
        s.PlaylistID,
        s.ContentID,
        s.Priority
    FROM Schedules s
    LEFT JOIN ScheduleAssignments sa ON s.ScheduleID = sa.ScheduleID
    LEFT JOIN ClientGroupMembership cgm ON sa.GroupID = cgm.GroupID
    WHERE s.Enabled = 1
      AND s.Deleted = 0
      AND (sa.ClientID = @ClientID OR cgm.ClientID = @ClientID)
      AND (s.StartDateTime IS NULL OR s.StartDateTime <= @CurrentDateTime)
      AND (s.EndDateTime IS NULL OR s.EndDateTime >= @CurrentDateTime)
      AND (s.TimeStart IS NULL OR CAST(@CurrentDateTime AS TIME) >= s.TimeStart)
      AND (s.TimeEnd IS NULL OR CAST(@CurrentDateTime AS TIME) <= s.TimeEnd)
    ORDER BY s.Priority DESC, s.ScheduleID
);
GO
```

---

## Indexes & Performance Optimization

### Additional Indexes
```sql
-- Content search optimization
CREATE INDEX IX_Content_FileSize ON Content(FileSize) WHERE Deleted = 0;
CREATE INDEX IX_Content_AspectRatio ON Content(AspectRatio) WHERE Deleted = 0;

-- Client performance
CREATE INDEX IX_Clients_Approved ON Clients(Approved) WHERE Approved = 0;
CREATE INDEX IX_Clients_CertExpiry ON Clients(CertificateExpiry) WHERE CertificateExpiry > SYSUTCDATETIME();

-- Audit log partitioning (by month)
CREATE PARTITION FUNCTION PF_AuditLog_Monthly (DATETIME2(7))
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01', 
    '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
    '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01'
);
GO

CREATE PARTITION SCHEME PS_AuditLog_Monthly
AS PARTITION PF_AuditLog_Monthly
ALL TO ([PRIMARY]);
GO

-- Apply partition scheme to AuditLog
-- (Requires recreating table with partition scheme)
```

### Statistics Maintenance
```sql
-- Auto-update statistics
ALTER DATABASE MakerScreen SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE MakerScreen SET AUTO_UPDATE_STATISTICS_ASYNC ON;

-- Create statistics for frequently filtered columns
CREATE STATISTICS STAT_Content_Tags ON Content(Tags);
CREATE STATISTICS STAT_Clients_Location ON Clients(LocationName, Building, Floor);
```

---

## Backup & Maintenance

### Backup Strategy
```sql
-- Full backup daily
BACKUP DATABASE MakerScreen
TO DISK = 'D:\Backups\MakerScreen_Full.bak'
WITH COMPRESSION, CHECKSUM, INIT;

-- Transaction log backup hourly
BACKUP LOG MakerScreen
TO DISK = 'D:\Backups\MakerScreen_Log.trn'
WITH COMPRESSION, CHECKSUM, NOINIT;
```

### Maintenance Jobs
```sql
-- Index maintenance (weekly)
ALTER INDEX ALL ON Content REBUILD WITH (ONLINE = ON);
ALTER INDEX ALL ON Clients REBUILD WITH (ONLINE = ON);
ALTER INDEX ALL ON AuditLog REORGANIZE;

-- Update statistics (weekly)
UPDATE STATISTICS Content WITH FULLSCAN;
UPDATE STATISTICS Clients WITH FULLSCAN;
UPDATE STATISTICS AuditLog WITH SAMPLE 10 PERCENT;

-- Archive old audit logs (monthly)
EXEC sp_ArchiveOldAuditLogs;

-- Clean old metrics (keep 90 days)
DELETE FROM ClientMetrics 
WHERE Timestamp < DATEADD(DAY, -90, SYSUTCDATETIME());
```

---

## Initial Data

### Default Roles & Permissions
```sql
-- Insert default roles
INSERT INTO Roles (RoleName, Description, IsSystemRole)
VALUES 
    ('Administrator', 'Full system access', 1),
    ('Content Manager', 'Manage content and schedules', 1),
    ('Operator', 'Monitor and control clients', 1),
    ('Viewer', 'Read-only access', 1),
    ('Client', 'Device access only', 1);

-- Insert default permissions
INSERT INTO Permissions (PermissionName, Resource, Action, Description)
VALUES
    -- Content permissions
    ('content:create', 'Content', 'CREATE', 'Upload new content'),
    ('content:read', 'Content', 'READ', 'View content'),
    ('content:update', 'Content', 'UPDATE', 'Modify content'),
    ('content:delete', 'Content', 'DELETE', 'Delete content'),
    
    -- Client permissions
    ('client:register', 'Client', 'CREATE', 'Register new clients'),
    ('client:read', 'Client', 'READ', 'View clients'),
    ('client:update', 'Client', 'UPDATE', 'Modify client settings'),
    ('client:reboot', 'Client', 'EXECUTE', 'Reboot clients'),
    ('client:delete', 'Client', 'DELETE', 'Remove clients'),
    
    -- User permissions
    ('user:create', 'User', 'CREATE', 'Create users'),
    ('user:read', 'User', 'READ', 'View users'),
    ('user:update', 'User', 'UPDATE', 'Modify users'),
    ('user:delete', 'User', 'DELETE', 'Delete users'),
    ('user:assign_role', 'User', 'EXECUTE', 'Assign roles to users'),
    
    -- System permissions
    ('system:configure', 'System', 'UPDATE', 'Configure system settings'),
    ('audit:read', 'AuditLog', 'READ', 'View audit logs'),
    ('schedule:create', 'Schedule', 'CREATE', 'Create schedules'),
    ('schedule:read', 'Schedule', 'READ', 'View schedules'),
    ('schedule:update', 'Schedule', 'UPDATE', 'Modify schedules'),
    ('schedule:delete', 'Schedule', 'DELETE', 'Delete schedules');

-- Assign permissions to Administrator role
INSERT INTO RolePermissions (RoleID, PermissionID)
SELECT 
    (SELECT RoleID FROM Roles WHERE RoleName = 'Administrator'),
    PermissionID
FROM Permissions;

-- Assign permissions to Content Manager role
INSERT INTO RolePermissions (RoleID, PermissionID)
SELECT 
    (SELECT RoleID FROM Roles WHERE RoleName = 'Content Manager'),
    PermissionID
FROM Permissions
WHERE PermissionName IN (
    'content:create', 'content:read', 'content:update', 'content:delete',
    'schedule:create', 'schedule:read', 'schedule:update', 'schedule:delete',
    'client:read'
);

-- Assign permissions to Operator role
INSERT INTO RolePermissions (RoleID, PermissionID)
SELECT 
    (SELECT RoleID FROM Roles WHERE RoleName = 'Operator'),
    PermissionID
FROM Permissions
WHERE PermissionName IN (
    'content:read', 'client:read', 'client:reboot', 'schedule:read'
);

-- Assign permissions to Viewer role
INSERT INTO RolePermissions (RoleID, PermissionID)
SELECT 
    (SELECT RoleID FROM Roles WHERE RoleName = 'Viewer'),
    PermissionID
FROM Permissions
WHERE PermissionName IN ('content:read', 'client:read', 'schedule:read');
```

---

**Document Version**: 1.0  
**Last Updated**: 2024-01-15  
**Schema Version**: 1.0.0
