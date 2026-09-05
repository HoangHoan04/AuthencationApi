using AuthApi.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Infrastructure.Services;

public class UserAccessService : IUserAccessService
{
    private readonly IApplicationDbContext _context;

    public UserAccessService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserAccessSnapshot> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleRows = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => new { ur.Role!.Code, Permissions = ur.Role.RolePermissions.Select(rp => rp.Permission!.Code) })
            .ToListAsync(cancellationToken);

        var roles = roleRows.Select(r => r.Code).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

        var permissions = roleRows.SelectMany(r => r.Permissions).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();

        var apps = await _context.UserApps
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.IsActive && ua.RevokedAt == null)
            .Select(ua => ua.App!.Code)
            .ToListAsync(cancellationToken);

        return new UserAccessSnapshot
        {
            Roles = roles,
            Permissions = permissions,
            Apps = apps.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList()
        };
    }
}
