using AuthApi.Application.Features.Rbac;
using AuthApi.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
[Route("api/admin")]
public class RbacController : ControllerBase
{
    private readonly IRbacService _rbac;

    public RbacController(IRbacService rbac)
    {
        _rbac = rbac;
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles() => Ok(await _rbac.GetRolesAsync());

    [HttpPost("roles")]
    [HasPermission("AUTH:ROLE:MANAGE")]
    public async Task<IActionResult> CreateRole([FromBody] SaveRoleRequest request) =>
        Ok(await _rbac.SaveRoleAsync(null, request));

    [HttpPut("roles/{id:guid}")]
    [HasPermission("AUTH:ROLE:MANAGE")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] SaveRoleRequest request) =>
        Ok(await _rbac.SaveRoleAsync(id, request));

    [HttpDelete("roles/{id:guid}")]
    [HasPermission("AUTH:ROLE:MANAGE")]
    public async Task<IActionResult> DeleteRole(Guid id) =>
        Ok(new { success = await _rbac.DeleteRoleAsync(id) });

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions() => Ok(await _rbac.GetPermissionsAsync());

    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit([FromQuery] int limit = 200) =>
        Ok(await _rbac.GetAuditLogsAsync(limit));
}
