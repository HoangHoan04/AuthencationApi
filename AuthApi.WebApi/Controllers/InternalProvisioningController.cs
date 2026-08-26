using AuthApi.Application.DTOs.Users;
using AuthApi.Application.Features.Users;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

/// <summary>
/// Internal provisioning endpoint - called by other microservices (e.g. HrmApi) using an API key.
/// Not exposed to end users. Protected by X-Internal-Api-Key header.
/// </summary>
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
        if (string.IsNullOrWhiteSpace(expectedKey)) return false;
        Request.Headers.TryGetValue("X-Internal-Api-Key", out var provided);
        return provided == expectedKey;
    }

    /// <summary>
    /// Tạo tài khoản người dùng từ HRM (khi nhân viên mới được thêm vào).
    /// Nếu email đã tồn tại thì trả về UserId hiện có.
    /// </summary>
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

            var user = await _userService.CreateUserAsync(new CreateUserRequest
            {
                Email = request.Email,
                FullName = request.FullName,
                Phone = request.Phone,
                CompanyId = request.CompanyId,
                Password = request.DefaultPassword ?? GenerateTemporaryPassword(),
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

    private static string GenerateTemporaryPassword()
    {
        var chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#";
        var random = new Random();
        return new string(Enumerable.Range(0, 12).Select(_ => chars[random.Next(chars.Length)]).ToArray());
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

public class ProvisionEmployeeResponse
{
    public Guid UserId { get; set; }
    public bool IsNew { get; set; }
}
