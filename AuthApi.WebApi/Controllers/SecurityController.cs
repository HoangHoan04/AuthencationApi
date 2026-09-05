using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Security;
using AuthApi.Application.Features.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
public class SecurityController : ControllerBase
{
    private readonly ISecurityService _securityService;
    private readonly IRsaKeyManager _rsaKeyManager;

    public SecurityController(ISecurityService securityService, IRsaKeyManager rsaKeyManager)
    {
        _securityService = securityService;
        _rsaKeyManager = rsaKeyManager;
    }

    [HttpGet("api/admin/sessions")]
    public async Task<ActionResult<List<SessionDto>>> GetSessions([FromQuery] Guid? userId, [FromQuery] bool includeRevoked = false)
    {
        var sessions = await _securityService.GetSessionsAsync(userId, includeRevoked);
        return Ok(sessions);
    }

    [HttpPost("api/admin/sessions/{id:guid}/revoke")]
    public async Task<IActionResult> RevokeSession(Guid id)
    {
        var success = await _securityService.RevokeSessionAsync(id, null);
        return Ok(new { success });
    }

    [HttpGet("api/admin/logs")]
    public async Task<ActionResult<List<LoginHistoryDto>>> GetLogs([FromQuery] int limit = 100)
    {
        var logs = await _securityService.GetLoginHistoriesAsync(limit);
        return Ok(logs);
    }

    [HttpGet("api/admin/security/keys")]
    public async Task<ActionResult<List<SecurityKeyDto>>> GetSecurityKeys()
    {
        return Ok(await _securityService.GetSigningKeysAsync());
    }

    [HttpPost("api/admin/security/keys/rotate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RotateKeys()
    {
        await _rsaKeyManager.RotateAsync();
        return Ok(new { success = true, keys = await _securityService.GetSigningKeysAsync() });
    }
}
