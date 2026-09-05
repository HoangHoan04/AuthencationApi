using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingService _settingService;

    public SystemSettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ cấu hình động trong hệ thống
    /// </summary>
    [HttpGet("api/admin/settings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<List<SystemSettingDto>>> GetAllSettings()
    {
        var settings = await _settingService.GetAllSettingsAsync();
        return Ok(settings);
    }

    /// <summary>
    /// Lấy cấu hình bảo mật & xác thực (bao gồm trạng thái bật/tắt 2FA)
    /// </summary>
    [HttpGet("api/admin/settings/security")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<SecuritySettingsDto>> GetSecuritySettings()
    {
        var config = await _settingService.GetSecuritySettingsAsync();
        return Ok(config);
    }

    /// <summary>
    /// Cập nhật cấu hình bảo mật & xác thực (bật/tắt 2FA, timeout, v.v.)
    /// </summary>
    [HttpPut("api/admin/settings/security")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<SecuritySettingsDto>> UpdateSecuritySettings([FromBody] UpdateSecuritySettingsDto request)
    {
        var updated = await _settingService.UpdateSecuritySettingsAsync(request);
        return Ok(updated);
    }

    /// <summary>
    /// Cập nhật một cấu hình cụ thể theo Key
    /// </summary>
    [HttpPut("api/admin/settings/{key}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<SystemSettingDto>> UpdateSetting(string key, [FromBody] UpdateSingleSettingRequest request)
    {
        var updated = await _settingService.SetSettingAsync(key, request.Value, request.Description, request.Group ?? "General", request.ValueType ?? "string");
        return Ok(updated);
    }

    /// <summary>
    /// Lấy cấu hình công khai cho màn hình xác thực (không yêu cầu token)
    /// </summary>
    [HttpGet("api/admin/settings/public-auth-config")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicAuthConfig()
    {
        var is2FaEnabled = await _settingService.IsTwoFactorAuthEnabledAsync();
        return Ok(new
        {
            enableTwoFactorAuth = is2FaEnabled
        });
    }
}

public class UpdateSingleSettingRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
    public string? ValueType { get; set; }
}
