using AuthApi.Application.DTOs.Users;

namespace AuthApi.Application.DTOs.Auth;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? ReturnUrl { get; set; }

    public string EffectiveEmail => !string.IsNullOrWhiteSpace(Email) ? Email : (Username ?? string.Empty);
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string Token => AccessToken;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 900;
    public string TokenType { get; set; } = "Bearer";
    public UserProfileDto User { get; set; } = null!;

    public string Username => User?.Email ?? string.Empty;
    public string Email => User?.Email ?? string.Empty;
    public List<string> Roles => User != null ? new List<string> { User.Role } : new();
    public List<string> Permissions => new() { "HOME:VIEW", "USER:VIEW", "SYSTEM:ADMIN" };
    public bool RequiresTwoFactor { get; set; } = false;
    public string? TempToken { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class TwoFactorSetupResponse
{
    public string SecretKey { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
}

public class VerifyTwoFactorRequest
{
    public string TempToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
