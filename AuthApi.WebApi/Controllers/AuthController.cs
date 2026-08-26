using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Auth;
using AuthApi.Application.DTOs.Security;
using AuthApi.Application.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/admin/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(
                request,
                _currentUserService.IpAddress,
                _currentUserService.UserAgent
            );
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(
                request,
                _currentUserService.IpAddress,
                _currentUserService.UserAgent
            );
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
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

        var success = await _authService.ChangePasswordAsync(_currentUserService.UserId.Value, request);
        return Ok(new { success });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var resetToken = await _authService.ForgotPasswordAsync(request);
        return Ok(new { success = true, resetToken });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var success = await _authService.ResetPasswordAsync(request);
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
