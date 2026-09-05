namespace AuthApi.Application.DTOs.Settings;

public class SystemSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = "General";
    public string ValueType { get; set; } = "string";
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class SecuritySettingsDto
{
    public bool EnableTwoFactorAuth { get; set; }
    public int SessionTimeoutMinutes { get; set; } = 60;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int AccountLockoutMinutes { get; set; } = 15;
    public bool RequirePasswordChangeOnFirstLogin { get; set; } = true;
    public int PasswordMinLength { get; set; } = 8;
}

public class UpdateSecuritySettingsDto
{
    public bool EnableTwoFactorAuth { get; set; }
    public int? SessionTimeoutMinutes { get; set; }
    public int? MaxFailedLoginAttempts { get; set; }
    public int? AccountLockoutMinutes { get; set; }
    public bool? RequirePasswordChangeOnFirstLogin { get; set; }
    public int? PasswordMinLength { get; set; }
}
