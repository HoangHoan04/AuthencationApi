using AuthApi.Application.DTOs.Users;
using AuthApi.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
[Route("api/admin/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserProfileDto>>> GetUsers([FromQuery] string? search, [FromQuery] Guid? companyId)
    {
        var users = await _userService.GetUsersAsync(search, companyId);
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserProfileDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        return Ok(user);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserProfileDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateUserAsync(id, request);
        return Ok(user);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var success = await _userService.DeleteUserAsync(id);
        return Ok(new { success });
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(Guid id, [FromBody] ResetUserPasswordRequest request)
    {
        var success = await _userService.ResetUserPasswordAsync(id, request.NewPassword);
        return Ok(new { success });
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var success = await _userService.UnlockUserAsync(id);
        return Ok(new { success });
    }
}
