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
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public List<string> Apps { get; set; } = new();
    public bool RequiresTwoFactor { get; set; }
    public string? TempToken { get; set; }
    public bool MustChangePassword { get; set; }
    public bool MustEnrollMfa { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? DeviceName { get; set; }

    public string EffectiveRefreshToken => RefreshToken?.Trim() ?? string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string? OldPassword { get; set; }
    public string NewPassword { get; set; } = string.Empty;

    public string EffectiveCurrentPassword =>
        !string.IsNullOrWhiteSpace(CurrentPassword) ? CurrentPassword : (OldPassword ?? string.Empty);
}

public class VerifyTwoFactorRequest
{
    public string TempToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string? Otp { get; set; }
}

public class TwoFactorSetupResponse
{
    public string SecretKey { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
}

public class EnableTwoFactorRequest
{
    public string Code { get; set; } = string.Empty;
}

public class DisableTwoFactorRequest
{
    public string Code { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public class AcceptInviteRequest
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    public string Token { get; set; } = string.Empty;
}

public class OAuthAuthorizeCompleteRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? Scope { get; set; }
    public string? Nonce { get; set; }
}

public class OAuthTokenRequest
{
    public string GrantType { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? RedirectUri { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? CodeVerifier { get; set; }
    public string? RefreshToken { get; set; }
    public string? Scope { get; set; }
}
