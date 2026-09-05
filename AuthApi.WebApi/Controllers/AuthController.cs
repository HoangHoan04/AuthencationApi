using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Auth;
using AuthApi.Application.DTOs.Security;
using AuthApi.Application.DTOs.Users;
using AuthApi.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/admin/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService, IUserService userService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(
            request,
            _currentUserService.IpAddress,
            _currentUserService.UserAgent);
        return Ok(response);
    }

    [HttpPost("2fa/verify")]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<AuthResponse>> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        var response = await _authService.VerifyTwoFactorAsync(
            request,
            _currentUserService.IpAddress,
            _currentUserService.UserAgent);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(
            request,
            _currentUserService.IpAddress,
            _currentUserService.UserAgent);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var success = await _authService.LogoutAsync(request.RefreshToken);
        return Ok(new { success });
    }

    [Authorize]
    [HttpGet("me")]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var profile = await _authService.GetCurrentUserProfileAsync(_currentUserService.UserId.Value);
        return Ok(profile);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var response = await _authService.ChangePasswordAsync(
            _currentUserService.UserId.Value,
            request,
            _currentUserService.IpAddress,
            _currentUserService.UserAgent);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(new { success = true });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var success = await _authService.ResetPasswordAsync(request);
        return Ok(new { success });
    }

    [HttpPost("accept-invite")]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
    {
        var success = await _userService.AcceptInviteAsync(request.Token, request.Password);
        return Ok(new { success });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var success = await _userService.VerifyEmailAsync(request.Token);
        return Ok(new { success });
    }

    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<ActionResult<TwoFactorSetupResponse>> SetupTwoFactor()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        return Ok(await _authService.SetupTwoFactorAsync(_currentUserService.UserId.Value));
    }

    [Authorize]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var backupCodes = await _authService.EnableTwoFactorAsync(_currentUserService.UserId.Value, request.Code);
        return Ok(new { success = true, backupCodes });
    }

    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _authService.DisableTwoFactorAsync(_currentUserService.UserId.Value, request);
        return Ok(new { success });
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<ActionResult<List<SessionDto>>> GetSessions()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var sessions = await _authService.GetActiveSessionsAsync(_currentUserService.UserId.Value, null);
        return Ok(sessions);
    }

    [Authorize]
    [HttpDelete("sessions/{id:guid}")]
    public async Task<IActionResult> RevokeSession(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _authService.RevokeSessionAsync(_currentUserService.UserId.Value, id);
        return Ok(new { success });
    }

    [Authorize]
    [HttpDelete("sessions/revoke-others")]
    public async Task<IActionResult> RevokeOtherSessions([FromBody] RefreshTokenRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _authService.RevokeAllOtherSessionsAsync(_currentUserService.UserId.Value, request.RefreshToken);
        return Ok(new { success });
    }
}
