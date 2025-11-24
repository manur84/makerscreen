namespace MakerScreen.Core.Models;

public class User
{
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public Guid? ADObjectGUID { get; set; }
    public byte[]? PasswordHash { get; set; }
    public byte[]? PasswordSalt { get; set; }
    public bool Enabled { get; set; } = true;
    public bool Locked { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LastFailedLogin { get; set; }
    public DateTime? LastSuccessfulLogin { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class Role
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission
{
    public int PermissionID { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class UserRole
{
    public int UserID { get; set; }
    public int RoleID { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

public class RolePermission
{
    public int RoleID { get; set; }
    public int PermissionID { get; set; }
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
    
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
