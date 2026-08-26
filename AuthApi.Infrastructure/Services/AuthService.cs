using System.Security.Cryptography;
using System.Text;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Auth;
using AuthApi.Application.DTOs.Security;
using AuthApi.Application.DTOs.Users;
using AuthApi.Application.Mappings;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        var email = request.EffectiveEmail;
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
        {
            await LogLoginEventAsync(null, email, LoginEventType.LoginFailed, ipAddress, userAgent, "User not found");
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
        }

        if (user.Status == UserStatus.Disabled)
        {
            await LogLoginEventAsync(user.Id, request.Email, LoginEventType.LoginFailed, ipAddress, userAgent, "Account disabled");
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa.");
        }

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTimeOffset.UtcNow)
        {
            var remainingMin = Math.Ceiling((user.LockedUntil.Value - DateTimeOffset.UtcNow).TotalMinutes);
            await LogLoginEventAsync(user.Id, request.Email, LoginEventType.LoginFailed, ipAddress, userAgent, "Account temporarily locked");
            throw new UnauthorizedAccessException($"Tài khoản bị tạm khóa do nhập sai nhiều lần. Vui lòng thử lại sau {remainingMin} phút.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);
                await LogLoginEventAsync(user.Id, request.Email, LoginEventType.AccountLocked, ipAddress, userAgent, "Locked after 5 failed attempts");
            }
            else
            {
                await LogLoginEventAsync(user.Id, request.Email, LoginEventType.LoginFailed, ipAddress, userAgent, $"Invalid password (Attempt {user.FailedLoginAttempts})");
            }

            await _context.SaveChangesAsync();
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
        }

        // Reset failed login attempts on success
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var (rawRefreshToken, tokenHash) = _jwtTokenGenerator.GenerateRefreshToken();

        var familyId = Guid.NewGuid();
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            FamilyId = familyId,
            DeviceName = !string.IsNullOrWhiteSpace(request.DeviceName) ? request.DeviceName : "Web Browser",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await LogLoginEventAsync(user.Id, request.Email, LoginEventType.LoginSuccess, ipAddress, userAgent, null);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresIn = 900,
            TokenType = "Bearer",
            User = UserMapper.ToDto(user)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, string? userAgent)
    {
        var tokenHash = HashRefreshToken(request.RefreshToken);
        var storedToken = await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u!.Company)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken == null || storedToken.User == null)
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
        }

        // Detect Token Reuse / Replay Attack!
        if (storedToken.RevokedAt != null)
        {
            _logger.LogWarning("Refresh token reuse detected for User {UserId}, Family {FamilyId}. Revoking family tokens!",
                storedToken.UserId, storedToken.FamilyId);

            // Invalidate all tokens in the family
            var familyTokens = await _context.RefreshTokens
                .Where(t => t.FamilyId == storedToken.FamilyId && t.RevokedAt == null)
                .ToListAsync();

            foreach (var t in familyTokens)
            {
                t.RevokedAt = DateTimeOffset.UtcNow;
            }

            await LogLoginEventAsync(storedToken.UserId, storedToken.User.Email, LoginEventType.Logout, ipAddress, userAgent, "Token replay attack detected");
            await _context.SaveChangesAsync();

            throw new UnauthorizedAccessException("Phát hiện phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
        }

        if (DateTimeOffset.UtcNow >= storedToken.ExpiresAt)
        {
            throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }

        // Revoke the old token
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        // Generate new token pair with the same FamilyId (Token Rotation)
        var user = storedToken.User!;
        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var (newRawRefreshToken, newTokenHash) = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newTokenHash,
            FamilyId = storedToken.FamilyId,
            DeviceName = !string.IsNullOrWhiteSpace(request.DeviceName) ? request.DeviceName : storedToken.DeviceName,
            IpAddress = ipAddress ?? storedToken.IpAddress,
            UserAgent = userAgent ?? storedToken.UserAgent,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };

        storedToken.ReplacedByTokenId = newRefreshTokenEntity.Id;
        _context.RefreshTokens.Add(newRefreshTokenEntity);

        await LogLoginEventAsync(user.Id, user.Email, LoginEventType.TokenRefreshed, ipAddress, userAgent, null);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRawRefreshToken,
            ExpiresIn = 900,
            TokenType = "Bearer",
            User = UserMapper.ToDto(user)
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var token = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token != null && token.RevokedAt == null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            if (token.User != null)
            {
                await LogLoginEventAsync(token.UserId, token.User.Email, LoginEventType.Logout, token.IpAddress, token.UserAgent, "User logged out");
            }
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<List<SessionDto>> GetActiveSessionsAsync(Guid userId, string? currentRefreshToken)
    {
        string? currentHash = !string.IsNullOrWhiteSpace(currentRefreshToken)
            ? HashRefreshToken(currentRefreshToken)
            : null;

        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tokens.Select(t => new SessionDto
        {
            Id = t.Id,
            FamilyId = t.FamilyId,
            DeviceName = t.DeviceName ?? "Unknown Device",
            IpAddress = t.IpAddress,
            UserAgent = t.UserAgent,
            CreatedAt = t.CreatedAt,
            ExpiresAt = t.ExpiresAt,
            IsCurrent = currentHash != null && t.TokenHash == currentHash
        }).ToList();
    }

    public async Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId);

        if (token != null && token.RevokedAt == null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> RevokeAllOtherSessionsAsync(Guid userId, string currentRefreshToken)
    {
        var currentHash = HashRefreshToken(currentRefreshToken);
        var otherTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.TokenHash != currentHash && t.RevokedAt == null)
            .ToListAsync();

        foreach (var t in otherTokens)
        {
            t.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");
        }

        return UserMapper.ToDto(user);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng.");
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        // Revoke all active sessions on password change for security
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var t in activeTokens)
        {
            t.RevokedAt = DateTimeOffset.UtcNow;
        }

        await LogLoginEventAsync(user.Id, user.Email, LoginEventType.PasswordChanged, null, null, "Password changed by user");
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
        {
            // Do not disclose user existence
            return string.Empty;
        }

        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var tokenHash = HashRefreshToken(rawToken);

        var resetEntity = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(2),
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.PasswordResets.Add(resetEntity);
        await LogLoginEventAsync(user.Id, user.Email, LoginEventType.PasswordResetRequested, null, null, null);
        await _context.SaveChangesAsync();

        return rawToken;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var tokenHash = HashRefreshToken(request.Token);
        var reset = await _context.PasswordResets
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash && !r.IsUsed);

        if (reset == null || reset.User == null || reset.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Mã đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        reset.IsUsed = true;
        reset.User.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        // Revoke all existing sessions
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == reset.UserId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens)
        {
            t.RevokedAt = DateTimeOffset.UtcNow;
        }

        await LogLoginEventAsync(reset.UserId, reset.User.Email, LoginEventType.PasswordChanged, null, null, "Password reset via token");
        await _context.SaveChangesAsync();
        return true;
    }

    private static string HashRefreshToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes);
    }

    private async Task LogLoginEventAsync(
        Guid? userId,
        string email,
        LoginEventType eventType,
        string? ipAddress,
        string? userAgent,
        string? failureReason)
    {
        try
        {
            var log = new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EmailAttempted = email,
                EventType = eventType,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                FailureReason = failureReason,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.LoginHistories.Add(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record login history log.");
        }
    }
}
