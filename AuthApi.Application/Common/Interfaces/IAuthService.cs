using AuthApi.Application.DTOs.Auth;
using AuthApi.Application.DTOs.Security;
using AuthApi.Application.DTOs.Users;

namespace AuthApi.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, string? userAgent);
    Task<bool> LogoutAsync(string refreshToken);
    Task<List<SessionDto>> GetActiveSessionsAsync(Guid userId, string? currentRefreshToken);
    Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId);
    Task<bool> RevokeAllOtherSessionsAsync(Guid userId, string currentRefreshToken);
    Task<UserProfileDto> GetCurrentUserProfileAsync(Guid userId);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<string> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}
