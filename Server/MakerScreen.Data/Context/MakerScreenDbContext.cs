using Microsoft.EntityFrameworkCore;
using MakerScreen.Core.Models;

namespace MakerScreen.Data.Context;

public class MakerScreenDbContext : DbContext
{
    public MakerScreenDbContext(DbContextOptions<MakerScreenDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Content> Contents { get; set; }
    public DbSet<Playlist> Playlists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User-Role many-to-many
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserID, ur.RoleID });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserID);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleID);

        // Role-Permission many-to-many
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleID, rp.PermissionID });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleID);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionID);

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, RoleName = "Administrator", Description = "Full system access", IsSystemRole = true },
            new Role { RoleID = 2, RoleName = "Operator", Description = "Monitor and control clients", IsSystemRole = true },
            new Role { RoleID = 3, RoleName = "Viewer", Description = "Read-only access", IsSystemRole = true }
        );
    }
}
