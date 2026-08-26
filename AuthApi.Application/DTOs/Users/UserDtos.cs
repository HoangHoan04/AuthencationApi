using AuthApi.Domain.Enums;

namespace AuthApi.Application.DTOs.Users;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyCode { get; set; }
    public string? CompanyName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username => Email;
    public string? Phone { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public List<string> Roles => new() { Role };
    public List<string> Permissions => new() { "HOME:VIEW", "USER:VIEW", "SYSTEM:ADMIN" };
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; }
}

public class CreateUserRequest
{
    public Guid? CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "User";
}

public class UpdateUserRequest
{
    public Guid? CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
}

public class ResetUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
