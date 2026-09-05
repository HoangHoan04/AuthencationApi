using System.Security.Cryptography;
using System.Text;
using AuthApi.Application.DTOs.Users;
using AuthApi.Application.Features.Users;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Route("api/internal")]
public class InternalProvisioningController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InternalProvisioningController> _logger;

    public InternalProvisioningController(
        IUserService userService,
        IConfiguration configuration,
        ILogger<InternalProvisioningController> logger)
    {
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }

    private bool IsAuthorized()
    {
        var expectedKey = _configuration["InternalApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return false;
        }

        Request.Headers.TryGetValue("X-Internal-Api-Key", out var provided);
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey));
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided.ToString()));
        return CryptographicOperations.FixedTimeEquals(expectedHash, providedHash);
    }

    [HttpPost("provision-employee")]
    public async Task<ActionResult<ProvisionEmployeeResponse>> ProvisionEmployee(
        [FromBody] ProvisionEmployeeRequest request)
    {
        if (!IsAuthorized())
        {
            return Unauthorized(new { message = "Invalid or missing X-Internal-Api-Key." });
        }

        try
        {
            var existing = await _userService.GetUserByEmailAsync(request.Email);
            if (existing != null)
            {
                return Ok(new ProvisionEmployeeResponse { UserId = existing.Id, IsNew = false });
            }

            var user = await _userService.InviteAsync(new InviteUserRequest
            {
                Email = request.Email,
                FullName = request.FullName,
                Phone = request.Phone,
                CompanyId = request.CompanyId,
                Role = "User",
            });

            return Ok(new ProvisionEmployeeResponse { UserId = user.Id, IsNew = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision employee account for email {Email}", request.Email);
            return StatusCode(500, new { message = "Failed to provision account." });
        }
    }

    [HttpPost("deprovision-employee")]
    public async Task<IActionResult> DeprovisionEmployee([FromBody] DeprovisionEmployeeRequest request)
    {
        if (!IsAuthorized())
        {
            return Unauthorized(new { message = "Invalid or missing X-Internal-Api-Key." });
        }

        var success = await _userService.DeprovisionAsync(request.Email);
        return Ok(new { success });
    }
}

public class ProvisionEmployeeRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? CompanyId { get; set; }
    public string? DefaultPassword { get; set; }
}

public class DeprovisionEmployeeRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ProvisionEmployeeResponse
{
    public Guid UserId { get; set; }
    public bool IsNew { get; set; }
}
