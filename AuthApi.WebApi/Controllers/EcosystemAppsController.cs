using AuthApi.Application.DTOs.EcosystemApps;
using AuthApi.Application.Features.EcosystemApps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
[Route("api/apps")]
[Route("api/admin/apps")]
[Route("api/admin/[controller]")]
public class EcosystemAppsController : ControllerBase
{
    private readonly IEcosystemAppService _appService;

    public EcosystemAppsController(IEcosystemAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("public/by-client/{clientId}")]
    [AllowAnonymous]
    public async Task<ActionResult<EcosystemAppDto>> GetByClientId(string clientId)
    {
        var app = await _appService.GetByClientIdAsync(clientId);
        if (app == null)
        {
            return NotFound(new { message = "Ứng dụng không tồn tại trong hệ sinh thái." });
        }
        return Ok(app);
    }

    [HttpGet]
    public async Task<ActionResult<List<EcosystemAppDto>>> GetApps()
    {
        var apps = await _appService.GetAppsAsync();
        return Ok(apps);
    }

    [HttpPost]
    public async Task<ActionResult<EcosystemAppDto>> CreateApp([FromBody] CreateAppRequest request)
    {
        var app = await _appService.CreateAppAsync(request);
        return Ok(app);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EcosystemAppDto>> UpdateApp(Guid id, [FromBody] UpdateAppRequest request)
    {
        var app = await _appService.UpdateAppAsync(id, request);
        return Ok(app);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteApp(Guid id)
    {
        var success = await _appService.DeleteAppAsync(id);
        return Ok(new { success });
    }
}
