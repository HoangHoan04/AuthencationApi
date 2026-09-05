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
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public List<string> Apps { get; set; } = new();
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; }
    public bool MustChangePassword { get; set; }
    public bool MfaEnabled { get; set; }
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

public class InviteUserRequest
{
    public Guid? CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "User";
    public List<Guid>? AppIds { get; set; }
}

public class AssignUserAppsRequest
{
    public List<Guid> AppIds { get; set; } = new();
}

public class AssignUserRolesRequest
{
    public Guid RoleId { get; set; }
    public Guid? AppId { get; set; }
    public Guid? CompanyId { get; set; }
}

public class ResetUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
