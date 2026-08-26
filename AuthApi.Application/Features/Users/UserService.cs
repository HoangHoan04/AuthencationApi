using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Users;
using AuthApi.Application.Mappings;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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
}

public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<UserProfileDto>> GetUsersAsync(string? search, Guid? companyId)
    {
        var query = _context.Users
            .Include(u => u.Company)
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

        var role = !string.IsNullOrWhiteSpace(request.Role) ? request.Role : "User";
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Email = email,
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (user.CompanyId.HasValue)
        {
            user.Company = await _context.Companies.FindAsync(user.CompanyId.Value);
        }

        return UserMapper.ToDto(user);
    }

    public async Task<UserProfileDto> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == id);
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
            user.Role = request.Role;
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

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
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
}
