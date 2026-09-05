using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Settings;
using AuthApi.Domain.Entities.Security;
using AuthApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemSettingService> _logger;

    public const string KeyEnableTwoFactorAuth = "Security.EnableTwoFactorAuth";
    public const string KeySessionTimeoutMinutes = "Security.SessionTimeoutMinutes";
    public const string KeyMaxFailedLoginAttempts = "Security.MaxFailedLoginAttempts";
    public const string KeyAccountLockoutMinutes = "Security.AccountLockoutMinutes";
    public const string KeyRequirePasswordChangeOnFirstLogin = "Security.RequirePasswordChangeOnFirstLogin";
    public const string KeyPasswordMinLength = "Security.PasswordMinLength";

    public SystemSettingService(ApplicationDbContext context, ILogger<SystemSettingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsTwoFactorAuthEnabledAsync()
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == KeyEnableTwoFactorAuth);

        if (setting == null)
        {
            return false;
        }

        return bool.TryParse(setting.Value, out var enabled) && enabled;
    }

    public async Task<SecuritySettingsDto> GetSecuritySettingsAsync()
    {
        var settings = await _context.SystemSettings
            .AsNoTracking()
            .Where(s => s.Group == "Security")
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return new SecuritySettingsDto
        {
            EnableTwoFactorAuth = settings.TryGetValue(KeyEnableTwoFactorAuth, out var mfa) && bool.TryParse(mfa, out var mfaVal) && mfaVal,
            SessionTimeoutMinutes = settings.TryGetValue(KeySessionTimeoutMinutes, out var st) && int.TryParse(st, out var stVal) ? stVal : 60,
            MaxFailedLoginAttempts = settings.TryGetValue(KeyMaxFailedLoginAttempts, out var mfla) && int.TryParse(mfla, out var mflaVal) ? mflaVal : 5,
            AccountLockoutMinutes = settings.TryGetValue(KeyAccountLockoutMinutes, out var alm) && int.TryParse(alm, out var almVal) ? almVal : 15,
            RequirePasswordChangeOnFirstLogin = !settings.TryGetValue(KeyRequirePasswordChangeOnFirstLogin, out var rpc) || !bool.TryParse(rpc, out var rpcVal) || rpcVal,
            PasswordMinLength = settings.TryGetValue(KeyPasswordMinLength, out var pml) && int.TryParse(pml, out var pmlVal) ? pmlVal : 8
        };
    }

    public async Task<SecuritySettingsDto> UpdateSecuritySettingsAsync(UpdateSecuritySettingsDto dto)
    {
        await SetSettingInternalAsync(KeyEnableTwoFactorAuth, dto.EnableTwoFactorAuth.ToString().ToLowerInvariant(), "Bắt buộc xác thực 2 bước (MFA / 2FA) khi đăng nhập", "Security", "boolean");

        if (dto.SessionTimeoutMinutes.HasValue)
        {
            await SetSettingInternalAsync(KeySessionTimeoutMinutes, dto.SessionTimeoutMinutes.Value.ToString(), "Thời gian hết hạn phiên làm việc (phút)", "Security", "number");
        }
        if (dto.MaxFailedLoginAttempts.HasValue)
        {
            await SetSettingInternalAsync(KeyMaxFailedLoginAttempts, dto.MaxFailedLoginAttempts.Value.ToString(), "Số lần đăng nhập sai tối đa trước khi khóa", "Security", "number");
        }
        if (dto.AccountLockoutMinutes.HasValue)
        {
            await SetSettingInternalAsync(KeyAccountLockoutMinutes, dto.AccountLockoutMinutes.Value.ToString(), "Thời gian khóa tài khoản tạm thời (phút)", "Security", "number");
        }
        if (dto.RequirePasswordChangeOnFirstLogin.HasValue)
        {
            await SetSettingInternalAsync(KeyRequirePasswordChangeOnFirstLogin, dto.RequirePasswordChangeOnFirstLogin.Value.ToString().ToLowerInvariant(), "Bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên", "Security", "boolean");
        }
        if (dto.PasswordMinLength.HasValue)
        {
            await SetSettingInternalAsync(KeyPasswordMinLength, dto.PasswordMinLength.Value.ToString(), "Độ dài mật khẩu tối thiểu", "Security", "number");
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Security settings updated. EnableTwoFactorAuth: {EnableTwoFactorAuth}", dto.EnableTwoFactorAuth);

        return await GetSecuritySettingsAsync();
    }

    public async Task<List<SystemSettingDto>> GetAllSettingsAsync()
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .OrderBy(s => s.Group)
            .ThenBy(s => s.Key)
            .Select(s => new SystemSettingDto
            {
                Id = s.Id,
                Key = s.Key,
                Value = s.Value,
                Description = s.Description,
                Group = s.Group,
                ValueType = s.ValueType,
                UpdatedAt = s.UpdatedAt ?? s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<string> GetSettingValueAsync(string key, string defaultValue = "")
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key);

        return setting?.Value ?? defaultValue;
    }

    public async Task<SystemSettingDto> SetSettingAsync(string key, string value, string? description = null, string group = "General", string valueType = "string")
    {
        var setting = await SetSettingInternalAsync(key, value, description, group, valueType);
        await _context.SaveChangesAsync();

        return new SystemSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            Description = setting.Description,
            Group = setting.Group,
            ValueType = setting.ValueType,
            UpdatedAt = setting.UpdatedAt ?? setting.CreatedAt
        };
    }

    private async Task<SystemSetting> SetSettingInternalAsync(string key, string value, string? description, string group, string valueType)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = value,
                Description = description,
                Group = group,
                ValueType = valueType,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            if (description != null) setting.Description = description;
            setting.Group = group;
            setting.ValueType = valueType;
            setting.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return setting;
    }
}
