using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Rbac;
using AuthApi.Domain.Entities.Security;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Rbac;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Module { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

public class SaveRoleRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> PermissionCodes { get; set; } = new();
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public interface IRbacService
{
    Task<List<RoleDto>> GetRolesAsync();
    Task<List<PermissionDto>> GetPermissionsAsync();
    Task<RoleDto> SaveRoleAsync(Guid? id, SaveRoleRequest request);
    Task<bool> DeleteRoleAsync(Guid id);
    Task<List<AuditLogDto>> GetAuditLogsAsync(int limit = 200);
}

public class RbacService : IRbacService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;

    public RbacService(IApplicationDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await _context.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync();
        return roles.Select(Map).ToList();
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync()
    {
        return await _context.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Code)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Module = p.Module,
                Resource = p.Resource,
                Action = p.Action
            }).ToListAsync();
    }

    public async Task<RoleDto> SaveRoleAsync(Guid? id, SaveRoleRequest request)
    {
        Role role;
        if (id.HasValue)
        {
            role = await _context.Roles.Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id.Value)
                ?? throw new KeyNotFoundException("Không tìm thấy vai trò.");
            if (role.IsSystem)
            {
                role.Name = request.Name.Trim();
                role.Description = request.Description;
            }
            else
            {
                role.Code = request.Code.Trim();
                role.Name = request.Name.Trim();
                role.Description = request.Description;
            }
        }
        else
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                Code = request.Code.Trim(),
                Name = request.Name.Trim(),
                Description = request.Description,
                IsSystem = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.Roles.Add(role);
        }

        var permissions = await _context.Permissions
            .Where(p => request.PermissionCodes.Contains(p.Code))
            .ToListAsync();
        _context.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var permission in permissions)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                PermissionId = permission.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        await _audit.WriteAsync("Role", role.Id, AuditEventType.PermissionChanged, $"Cập nhật role {role.Code}");
        return Map(await _context.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstAsync(r => r.Id == role.Id));
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy vai trò.");
        if (role.IsSystem)
        {
            throw new InvalidOperationException("Không xóa được role hệ thống.");
        }

        role.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AuditLogDto>> GetAuditLogsAsync(int limit = 200)
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                ActorUserId = a.ActorUserId,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EventType = a.EventType.ToString(),
                Summary = a.Summary,
                CreatedAt = a.CreatedAt
            }).ToListAsync();
    }

    private static RoleDto Map(Role role) => new()
    {
        Id = role.Id,
        Code = role.Code,
        Name = role.Name,
        Description = role.Description,
        IsSystem = role.IsSystem,
        IsActive = role.IsActive,
        Permissions = role.RolePermissions.Select(rp => rp.Permission?.Code ?? "").Where(c => c != "").ToList()
    };
}
