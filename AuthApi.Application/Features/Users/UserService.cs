using AuthApi.Application.Common;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Users;
using AuthApi.Application.Mappings;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.Rbac;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace AuthApi.Application.Features.Users;

public interface IUserService
{
    Task<List<UserProfileDto>> GetUsersAsync(string? search, Guid? companyId);
    Task<UserProfileDto?> GetUserByEmailAsync(string email);
    Task<UserProfileDto> CreateUserAsync(CreateUserRequest request);
    Task<UserProfileDto> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(Guid id);
    Task<bool> ResetUserPasswordAsync(Guid id, string newPassword);
    Task<bool> UnlockUserAsync(Guid id);
    Task<UserProfileDto> InviteAsync(InviteUserRequest request);
    Task<bool> AcceptInviteAsync(string token, string password);
    Task<bool> VerifyEmailAsync(string token);
    Task AssignAppsAsync(Guid userId, IReadOnlyCollection<Guid> appIds);
    Task AssignRoleAsync(Guid userId, AssignUserRolesRequest request);
    Task<bool> DeprovisionAsync(string email);
    Task ForceLogoutAsync(Guid userId);
    Task<List<Guid>> GetAssignedAppIdsAsync(Guid userId);
}

public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicy _passwordPolicy;
    private readonly IEmailSender _email;
    private readonly IConfiguration _configuration;
    private readonly ITokenDenylist _denylist;

    public UserService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IPasswordPolicy passwordPolicy,
        IEmailSender email,
        IConfiguration configuration,
        ITokenDenylist denylist)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _email = email;
        _configuration = configuration;
        _denylist = denylist;
    }

    public async Task<List<UserProfileDto>> GetUsersAsync(string? search, Guid? companyId)
    {
        var query = _context.Users
            .Include(u => u.Company)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(s) || u.FullName.ToLower().Contains(s) || (u.Phone != null && u.Phone.Contains(s)));
        }

        if (companyId.HasValue)
        {
            query = query.Where(u => u.CompanyId == companyId.Value);
        }

        var users = await query.ToListAsync();
        return users.Select(UserMapper.ToDto).ToList();
    }

    public async Task<UserProfileDto?> GetUserByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.Company)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted);
        return user == null ? null : UserMapper.ToDto(user);
    }

    public async Task<UserProfileDto> CreateUserAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        if (exists)
        {
            throw new InvalidOperationException("Email người dùng đã tồn tại trên hệ thống.");
        }

        var role = !string.IsNullOrWhiteSpace(request.Role) ? request.Role : RoleCodes.User;
        _passwordPolicy.Validate(request.Password);
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Email = email,
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            MustChangePassword = true,
            PasswordChangedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);
        await AssignPrimaryRoleAsync(user.Id, role, user.CompanyId);
        await _context.SaveChangesAsync();

        if (user.CompanyId.HasValue)
        {
            user.Company = await _context.Companies.FindAsync(user.CompanyId.Value);
        }

        return UserMapper.ToDto(user);
    }

    public async Task<UserProfileDto> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Company)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng.");
        }

        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone?.Trim();
        user.CompanyId = request.CompanyId;
        user.Status = request.Status;

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            await AssignPrimaryRoleAsync(user.Id, request.Role, request.CompanyId);
        }

        await _context.SaveChangesAsync();

        if (user.CompanyId.HasValue && user.Company == null)
        {
            user.Company = await _context.Companies.FindAsync(user.CompanyId.Value);
        }

        return UserMapper.ToDto(user);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user != null)
        {
            user.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> ResetUserPasswordAsync(Guid id, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng.");
        }

        _passwordPolicy.Validate(newPassword);
        _context.PasswordHistories.Add(new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = user.PasswordHash,
            CreatedAt = DateTimeOffset.UtcNow
        });
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.MustChangePassword = true;
        user.PasswordChangedAt = DateTimeOffset.UtcNow;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnlockUserAsync(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng.");
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        if (user.Status == UserStatus.Locked)
        {
            user.Status = UserStatus.Active;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserProfileDto> InviteAsync(InviteUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        if (exists)
        {
            throw new InvalidOperationException("Email đã tồn tại.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Email = email,
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            PasswordHash = _passwordHasher.HashPassword(Guid.NewGuid().ToString("N") + "Aa!1"),
            Status = UserStatus.Invited,
            MustChangePassword = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _context.Users.Add(user);
        await AssignPrimaryRoleAsync(
            user.Id,
            string.IsNullOrWhiteSpace(request.Role) ? RoleCodes.User : request.Role,
            user.CompanyId);

        if (request.AppIds != null)
        {
            foreach (var appId in request.AppIds.Distinct())
            {
                _context.UserApps.Add(new UserApp
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    AppId = appId,
                    IsActive = true,
                    GrantedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _context.EmailVerifications.Add(new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = email,
            TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(3),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync();

        var publicBase = _configuration["Auth:PublicBaseUrl"] ?? "http://localhost:4300";
        var link = $"{publicBase.TrimEnd('/')}/auth/accept-invite?token={raw}";
        await _email.SendAsync(email, "Lời mời tài khoản",
            $"<p>Bạn được mời vào hệ thống. Đặt mật khẩu tại:</p><p><a href=\"{link}\">{link}</a></p>");

        return UserMapper.ToDto(user);
    }

    public async Task<bool> AcceptInviteAsync(string token, string password)
    {
        _passwordPolicy.Validate(password);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
        var invite = await _context.EmailVerifications.Include(v => v.User)
            .FirstOrDefaultAsync(v => v.TokenHash == hash && v.ConsumedAt == null);
        if (invite?.User == null || invite.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Lời mời không hợp lệ hoặc đã hết hạn.");
        }

        invite.ConsumedAt = DateTimeOffset.UtcNow;
        invite.User.PasswordHash = _passwordHasher.HashPassword(password);
        invite.User.Status = UserStatus.Active;
        invite.User.EmailVerifiedAt = DateTimeOffset.UtcNow;
        invite.User.MustChangePassword = false;
        invite.User.PasswordChangedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
        var item = await _context.EmailVerifications.Include(v => v.User)
            .FirstOrDefaultAsync(v => v.TokenHash == hash && v.ConsumedAt == null);
        if (item?.User == null || item.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Token xác thực không hợp lệ.");
        }

        item.ConsumedAt = DateTimeOffset.UtcNow;
        item.User.EmailVerifiedAt = DateTimeOffset.UtcNow;
        if (item.User.Status == UserStatus.PendingVerification)
        {
            item.User.Status = UserStatus.Active;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task AssignAppsAsync(Guid userId, IReadOnlyCollection<Guid> appIds)
    {
        var existing = await _context.UserApps.Where(ua => ua.UserId == userId).ToListAsync();
        foreach (var row in existing)
        {
            row.IsActive = appIds.Contains(row.AppId);
            row.RevokedAt = row.IsActive ? null : DateTimeOffset.UtcNow;
        }

        foreach (var appId in appIds.Except(existing.Select(e => e.AppId)))
        {
            _context.UserApps.Add(new UserApp
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AppId = appId,
                IsActive = true,
                GrantedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task AssignRoleAsync(Guid userId, AssignUserRolesRequest request)
    {
        _context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = request.RoleId,
            AppId = request.AppId,
            CompanyId = request.CompanyId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeprovisionAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant());
        if (user == null)
        {
            return false;
        }

        user.Status = UserStatus.Disabled;
        var apps = await _context.UserApps.Where(a => a.UserId == user.Id).ToListAsync();
        foreach (var app in apps)
        {
            app.IsActive = false;
            app.RevokedAt = DateTimeOffset.UtcNow;
        }

        await ForceLogoutAsync(user.Id);
        return true;
    }

    public async Task ForceLogoutAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
        foreach (var t in tokens)
        {
            t.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _denylist.RevokeUserAsync(userId, DateTimeOffset.UtcNow.AddMinutes(15), "force-logout");
        await _context.SaveChangesAsync();
    }

    public async Task<List<Guid>> GetAssignedAppIdsAsync(Guid userId)
    {
        return await _context.UserApps
            .Where(ua => ua.UserId == userId && ua.IsActive && ua.RevokedAt == null)
            .Select(ua => ua.AppId)
            .ToListAsync();
    }

    private async Task AssignPrimaryRoleAsync(Guid userId, string roleCode, Guid? companyId)
    {
        var code = string.IsNullOrWhiteSpace(roleCode) ? RoleCodes.User : roleCode.Trim();
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Code == code)
                   ?? await _context.Roles.FirstAsync(r => r.Code == RoleCodes.Viewer);

        var existing = await _context.UserRoles
            .Where(ur => ur.UserId == userId && ur.AppId == null)
            .ToListAsync();
        var keep = existing.FirstOrDefault(e => e.RoleId == role.Id);
        foreach (var row in existing.Where(e => keep == null || e.Id != keep.Id))
        {
            row.IsDeleted = true;
        }

        if (keep != null)
        {
            keep.CompanyId = companyId;
            return;
        }

        _context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = role.Id,
            CompanyId = companyId,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
