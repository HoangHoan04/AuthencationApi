using System.Security.Cryptography;
using System.Text;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Application.DTOs.Auth;
using AuthApi.Application.DTOs.Security;
using AuthApi.Application.DTOs.Users;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;
using AuthApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace AuthApi.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUserAccessService _access;
    private readonly IPasswordPolicy _passwordPolicy;
    private readonly IEmailSender _email;
    private readonly IDataProtectionService _protection;
    private readonly ITokenDenylist _denylist;
    private readonly ISystemSettingService _systemSettingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUserAccessService access,
        IPasswordPolicy passwordPolicy,
        IEmailSender email,
        IDataProtectionService protection,
        ITokenDenylist denylist,
        ISystemSettingService systemSettingService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _access = access;
        _passwordPolicy = passwordPolicy;
        _email = email;
        _protection = protection;
        _denylist = denylist;
        _systemSettingService = systemSettingService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        var email = request.EffectiveEmail.Trim().ToLowerInvariant();
        var user = await _context.Users.Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null)
        {
            await LogLoginEventAsync(null, email, LoginEventType.LoginFailed, ipAddress, userAgent, "User not found");
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
        }

        if (user.Status is UserStatus.Disabled or UserStatus.Invited)
        {
            await LogLoginEventAsync(user.Id, email, LoginEventType.LoginFailed, ipAddress, userAgent, "Account not active");
            throw new UnauthorizedAccessException("Tài khoản chưa kích hoạt hoặc đã bị vô hiệu hóa.");
        }

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTimeOffset.UtcNow)
        {
            var remainingMin = Math.Ceiling((user.LockedUntil.Value - DateTimeOffset.UtcNow).TotalMinutes);
            throw new UnauthorizedAccessException($"Tài khoản bị tạm khóa. Thử lại sau {remainingMin} phút.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);
                await LogLoginEventAsync(user.Id, email, LoginEventType.AccountLocked, ipAddress, userAgent, "Locked after 5 failed attempts");
            }
            else
            {
                await LogLoginEventAsync(user.Id, email, LoginEventType.LoginFailed, ipAddress, userAgent, "Invalid password");
            }

            await _context.SaveChangesAsync();
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        var isTwoFactorGloballyEnabled = await _systemSettingService.IsTwoFactorAuthEnabledAsync();

        if (isTwoFactorGloballyEnabled && user.MfaEnabled)
        {
            await _context.SaveChangesAsync();
            return new AuthResponse
            {
                RequiresTwoFactor = true,
                TempToken = _jwtTokenGenerator.GenerateMfaTempToken(user.Id),
                User = UserProfileFactory.From(user)
            };
        }

        return await IssueSessionAsync(user, ipAddress, userAgent, request.DeviceName);
    }

    public async Task<AuthResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, string? ipAddress, string? userAgent)
    {
        var userId = _jwtTokenGenerator.ReadMfaTempUserId(request.TempToken)
            ?? throw new UnauthorizedAccessException("Phiên MFA không hợp lệ hoặc đã hết hạn.");

        var user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new UnauthorizedAccessException("Không tìm thấy người dùng.");

        if (!await VerifyMfaOrBackupAsync(user.Id, request.Code))
        {
            throw new UnauthorizedAccessException("Mã xác thực không hợp lệ.");
        }

        return await IssueSessionAsync(user, ipAddress, userAgent, null);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, string? userAgent)
    {
        var raw = request.EffectiveRefreshToken;
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
        }

        var tokenHash = HashToken(raw);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken == null)
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
        }

        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == storedToken.UserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
        }

        if (storedToken.RevokedAt != null)
        {
            var familyTokens = await _context.RefreshTokens
                .Where(t => t.FamilyId == storedToken.FamilyId && t.RevokedAt == null)
                .ToListAsync();
            foreach (var t in familyTokens)
            {
                t.RevokedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            throw new UnauthorizedAccessException("Phát hiện phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
        }

        if (DateTimeOffset.UtcNow >= storedToken.ExpiresAt)
        {
            throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn.");
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        var response = await IssueSessionAsync(user, ipAddress, userAgent, request.DeviceName, storedToken.FamilyId, storedToken.AppId);
        storedToken.ReplacedByTokenId = await _context.RefreshTokens
            .Where(t => t.UserId == storedToken.UserId && t.FamilyId == storedToken.FamilyId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync();
        await _context.SaveChangesAsync();
        return response;
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var token = await _context.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        if (token == null || token.RevokedAt != null)
        {
            return false;
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<SessionDto>> GetActiveSessionsAsync(Guid userId, string? currentRefreshToken)
    {
        string? currentHash = string.IsNullOrWhiteSpace(currentRefreshToken) ? null : HashToken(currentRefreshToken);
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
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId);
        if (token == null || token.RevokedAt != null)
        {
            return false;
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeAllOtherSessionsAsync(Guid userId, string currentRefreshToken)
    {
        var currentHash = HashToken(currentRefreshToken);
        var otherTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.TokenHash != currentHash && t.RevokedAt == null)
            .ToListAsync();
        foreach (var t in otherTokens)
        {
            t.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _denylist.RevokeUserAsync(userId, DateTimeOffset.UtcNow.AddMinutes(15), "revoke-others");
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(Guid userId)
    {
        var user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");
        var access = await _access.GetAsync(userId);
        return UserProfileFactory.From(user, access);
    }

    public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ipAddress, string? userAgent)
    {
        var user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (!_passwordHasher.VerifyPassword(request.EffectiveCurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        }

        await SetPasswordAsync(user, request.NewPassword);
        await RevokeUserSessionsAsync(userId, "password-changed");
        user.MustChangePassword = false;
        await LogLoginEventAsync(user.Id, user.Email, LoginEventType.PasswordChanged, ipAddress, userAgent, "Password changed by user");
        await _context.SaveChangesAsync();
        return await IssueSessionAsync(user, ipAddress, userAgent, "Web Browser");
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user == null)
        {
            return;
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _context.PasswordResets.Add(new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync();

        var publicBase = _configuration["Auth:PublicBaseUrl"] ?? "http://localhost:4300";
        var link = $"{publicBase.TrimEnd('/')}/auth/reset-password?token={rawToken}";
        await _email.SendAsync(user.Email, "Đặt lại mật khẩu",
            $"<p>Yêu cầu đặt lại mật khẩu. Liên kết hết hạn sau 30 phút:</p><p><a href=\"{link}\">{link}</a></p>");
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var token = request.Token?.Trim() ?? string.Empty;
        var reset = await _context.PasswordResets.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == HashToken(token) && !r.IsUsed);

        if (reset?.User == null || reset.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Mã đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        await SetPasswordAsync(reset.User, request.NewPassword);
        reset.IsUsed = true;
        reset.ConsumedAt = DateTimeOffset.UtcNow;
        await RevokeUserSessionsAsync(reset.UserId, "password-reset");
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TwoFactorSetupResponse> SetupTwoFactorAsync(Guid userId)
    {
        var user = await _context.Users.FirstAsync(u => u.Id == userId);
        var secret = KeyGeneration.GenerateRandomKey(20);
        var secretBase32 = Base32Encoding.ToString(secret);
        var issuer = Uri.EscapeDataString(_configuration["Jwt:Issuer"] ?? "Auth");
        var label = Uri.EscapeDataString(user.Email);

        var existing = await _context.MfaDevices.Where(d => d.UserId == userId && !d.IsVerified).ToListAsync();
        foreach (var device in existing)
        {
            device.IsDeleted = true;
        }

        _context.MfaDevices.Add(new MfaDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Method = MfaMethod.Totp,
            Name = "Authenticator",
            SecretEncrypted = _protection.Encrypt(secretBase32),
            IsVerified = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync();

        return new TwoFactorSetupResponse
        {
            SecretKey = secretBase32,
            ManualEntryKey = secretBase32,
            QrCodeUri = $"otpauth://totp/{issuer}:{label}?secret={secretBase32}&issuer={issuer}&digits=6&period=30"
        };
    }

    public async Task<IReadOnlyCollection<string>> EnableTwoFactorAsync(Guid userId, string code)
    {
        var device = await _context.MfaDevices
            .Where(d => d.UserId == userId && !d.IsVerified)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync() ?? throw new InvalidOperationException("Chưa khởi tạo MFA. Gọi setup trước.");

        var secret = _protection.Decrypt(device.SecretEncrypted);
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        if (!totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(1, 1)))
        {
            throw new InvalidOperationException("Mã xác thực không đúng.");
        }

        device.IsVerified = true;
        device.LastUsedAt = DateTimeOffset.UtcNow;
        var user = await _context.Users.FirstAsync(u => u.Id == userId);
        user.MfaEnabled = true;

        var backupCodes = Enumerable.Range(0, 10)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant())
            .ToList();
        var oldCodes = await _context.MfaBackupCodes.Where(c => c.UserId == userId).ToListAsync();
        _context.MfaBackupCodes.RemoveRange(oldCodes);
        foreach (var backup in backupCodes)
        {
            _context.MfaBackupCodes.Add(new MfaBackupCode
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CodeHash = _passwordHasher.HashPassword(backup),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return backupCodes;
    }

    public async Task<bool> DisableTwoFactorAsync(Guid userId, DisableTwoFactorRequest request)
    {
        var user = await _context.Users.FirstAsync(u => u.Id == userId);
        if (!string.IsNullOrWhiteSpace(request.Password) &&
            !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu không đúng.");
        }

        if (!await VerifyMfaOrBackupAsync(userId, request.Code))
        {
            throw new InvalidOperationException("Mã xác thực không đúng.");
        }

        user.MfaEnabled = false;
        var devices = await _context.MfaDevices.Where(d => d.UserId == userId).ToListAsync();
        foreach (var device in devices)
        {
            device.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> VerifyMfaOrBackupAsync(Guid userId, string code)
    {
        var trimmed = code.Trim();
        var devices = await _context.MfaDevices.Where(d => d.UserId == userId && d.IsVerified).ToListAsync();
        foreach (var device in devices)
        {
            var secret = _protection.Decrypt(device.SecretEncrypted);
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            if (totp.VerifyTotp(trimmed, out _, new VerificationWindow(1, 1)))
            {
                device.LastUsedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
        }

        var backups = await _context.MfaBackupCodes.Where(c => c.UserId == userId && c.UsedAt == null).ToListAsync();
        foreach (var backup in backups)
        {
            if (_passwordHasher.VerifyPassword(trimmed, backup.CodeHash))
            {
                backup.UsedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
        }

        return false;
    }

    private async Task<AuthResponse> IssueSessionAsync(
        User user,
        string? ipAddress,
        string? userAgent,
        string? deviceName,
        Guid? familyId = null,
        Guid? appId = null)
    {
        user.LastLoginAt = DateTimeOffset.UtcNow;
        var access = await _access.GetAsync(user.Id);
        var jti = Guid.NewGuid().ToString("N");
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, new AccessTokenClaims
        {
            Roles = access.Roles,
            Permissions = access.Permissions,
            Apps = access.Apps,
            Jti = jti
        });
        var (rawRefresh, refreshHash) = _jwtTokenGenerator.GenerateRefreshToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AppId = appId,
            CompanyId = user.CompanyId,
            TokenHash = refreshHash,
            FamilyId = familyId ?? Guid.NewGuid(),
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Web Browser" : deviceName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await LogLoginEventAsync(user.Id, user.Email, LoginEventType.LoginSuccess, ipAddress, userAgent, null);
        await _context.SaveChangesAsync();

        var profile = UserProfileFactory.From(user, access);
        var isTwoFactorGloballyEnabled = await _systemSettingService.IsTwoFactorAuthEnabledAsync();
        var mustEnrollMfa = isTwoFactorGloballyEnabled && !user.MfaEnabled &&
                            access.Roles.Any(r => r is "SuperAdmin" or "Admin");

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefresh,
            ExpiresIn = 900,
            User = profile,
            Roles = profile.Roles,
            Permissions = profile.Permissions,
            Apps = profile.Apps,
            MustChangePassword = user.MustChangePassword,
            MustEnrollMfa = mustEnrollMfa
        };
    }

    private async Task SetPasswordAsync(User user, string newPassword)
    {
        _passwordPolicy.Validate(newPassword);
        var history = await _context.PasswordHistories
            .Where(h => h.UserId == user.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Take(PasswordPolicy.HistoryCount)
            .ToListAsync();
        if (history.Any(h => _passwordHasher.VerifyPassword(newPassword, h.PasswordHash)) ||
            _passwordHasher.VerifyPassword(newPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Không được dùng lại mật khẩu gần đây.");
        }

        _context.PasswordHistories.Add(new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = user.PasswordHash,
            CreatedAt = DateTimeOffset.UtcNow
        });
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.PasswordChangedAt = DateTimeOffset.UtcNow;
    }

    private async Task RevokeUserSessionsAsync(Guid userId, string reason)
    {
        var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
        foreach (var t in tokens)
        {
            t.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _denylist.RevokeUserAsync(userId, DateTimeOffset.UtcNow.AddMinutes(15), reason);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
    }

    private async Task LogLoginEventAsync(
        Guid? userId,
        string email,
        LoginEventType eventType,
        string? ipAddress,
        string? userAgent,
        string? failureReason)
    {
        _context.LoginHistories.Add(new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmailAttempted = email,
            EventType = eventType,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            FailureReason = failureReason,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await Task.CompletedTask;
    }
}
