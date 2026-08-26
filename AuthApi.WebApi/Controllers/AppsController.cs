using AuthApi.Application.DTOs.EcosystemApps;
using AuthApi.Application.Features.EcosystemApps;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppsController : ControllerBase
{
    private readonly IEcosystemAppService _appService;

    public AppsController(IEcosystemAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<ActionResult<List<EcosystemAppDto>>> GetEcosystemApps()
    {
        var apps = await _appService.GetAppsAsync();
        return Ok(apps.Where(a => a.IsActive).ToList());
    }
}
