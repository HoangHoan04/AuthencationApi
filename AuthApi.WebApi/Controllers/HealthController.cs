using AuthApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public HealthController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("health/live")]
    [HttpGet("healthz")]
    public IActionResult Live() => Ok(new { status = "ok" });

    [HttpGet("health/ready")]
    [HttpGet("readyz")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Ok(new { status = "ok" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy" });
    }
}
