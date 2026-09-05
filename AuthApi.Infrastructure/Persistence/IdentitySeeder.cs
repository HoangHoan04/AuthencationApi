using AuthApi.Application.Common;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Rbac;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AuthApi.Infrastructure.Persistence;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedAdminAsync(context, passwordHasher, configuration, logger);
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        var existing = await context.Permissions.Select(p => p.Code).ToListAsync();
        foreach (var item in PermissionCodes.Catalog)
        {
            if (existing.Contains(item.Code))
            {
                continue;
            }

            context.Permissions.Add(new Permission
            {
                Id = Guid.NewGuid(),
                Code = item.Code,
                Name = item.Name,
                Module = item.Module,
                Resource = item.Resource,
                Action = item.Action,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        var permissions = await context.Permissions.ToListAsync();
        await EnsureRoleAsync(context, RoleCodes.SuperAdmin, "Super Admin", true, permissions.Select(p => p.Id));
        await EnsureRoleAsync(context, RoleCodes.Admin, "Admin công ty", true,
            permissions.Where(p => PermissionCodes.AdminPermissions.Contains(p.Code)).Select(p => p.Id));
        await EnsureRoleAsync(context, RoleCodes.Operator, "Operator", true,
            permissions.Where(p => PermissionCodes.OperatorPermissions.Contains(p.Code)).Select(p => p.Id));
        await EnsureRoleAsync(context, RoleCodes.Viewer, "Viewer", true,
            permissions.Where(p => PermissionCodes.ViewerPermissions.Contains(p.Code)).Select(p => p.Id));
        await EnsureRoleAsync(context, RoleCodes.User, "User", true,
            permissions.Where(p => PermissionCodes.UserPermissions.Contains(p.Code)).Select(p => p.Id));
    }

    private static async Task EnsureRoleAsync(
        ApplicationDbContext context,
        string code,
        string name,
        bool isSystem,
        IEnumerable<Guid> permissionIds)
    {
        var role = await context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Code == code);
        if (role == null)
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                IsSystem = isSystem,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
        }

        var existing = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var permissionId in permissionIds)
        {
            if (existing.Contains(permissionId))
            {
                continue;
            }

            context.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                PermissionId = permissionId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = (configuration["Seed:AdminEmail"] ?? "admin@company.com").Trim().ToLowerInvariant();
        var password = configuration["Seed:AdminPassword"] ?? "Admin@123456";

        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email = email,
                Phone = "0901234567",
                FullName = "Hệ thống Quản trị viên (Super Admin)",
                PasswordHash = passwordHasher.HashPassword(password),
                Status = UserStatus.Active,
                MustChangePassword = false,
                PasswordChangedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded SuperAdmin {Email}.", email);
        }
        else
        {
            user.PasswordHash = passwordHasher.HashPassword(password);
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.Status = UserStatus.Active;
            user.MustChangePassword = false;
            await context.SaveChangesAsync();
            logger.LogInformation("Ensured SuperAdmin password for {Email}.", email);
        }

        var superAdminRole = await context.Roles.FirstAsync(r => r.Code == RoleCodes.SuperAdmin);
        var hasRole = await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == superAdminRole.Id);
        if (!hasRole)
        {
            context.UserRoles.Add(new Domain.Entities.Rbac.UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = superAdminRole.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
