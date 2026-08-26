using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Auth;
using AuthApi.Application.DTOs.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public SessionsController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SessionDto>>> GetActiveSessions([FromQuery] string? currentRefreshToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var sessions = await _authService.GetActiveSessionsAsync(_currentUserService.UserId.Value, currentRefreshToken);
        return Ok(sessions);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeSession(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _authService.RevokeSessionAsync(_currentUserService.UserId.Value, id);
        return Ok(new { success });
    }

    [HttpPost("revoke-others")]
    public async Task<IActionResult> RevokeOthers([FromBody] RefreshTokenRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _authService.RevokeAllOtherSessionsAsync(_currentUserService.UserId.Value, request.RefreshToken);
        return Ok(new { success, message = "Đã đăng xuất khỏi tất cả các thiết bị khác." });
    }
}
