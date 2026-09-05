using AuthApi.Application.DTOs.Settings;

namespace AuthApi.Application.Common.Interfaces;

public interface ISystemSettingService
{
    Task<bool> IsTwoFactorAuthEnabledAsync();
    Task<SecuritySettingsDto> GetSecuritySettingsAsync();
    Task<SecuritySettingsDto> UpdateSecuritySettingsAsync(UpdateSecuritySettingsDto dto);
    Task<List<SystemSettingDto>> GetAllSettingsAsync();
    Task<string> GetSettingValueAsync(string key, string defaultValue = "");
    Task<SystemSettingDto> SetSettingAsync(string key, string value, string? description = null, string group = "General", string valueType = "string");
}
