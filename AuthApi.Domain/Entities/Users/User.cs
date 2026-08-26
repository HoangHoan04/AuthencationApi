using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Users;

public class User : BaseEntity
{
    public Guid? CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTimeOffset? LockedUntil { get; set; }

    public virtual Company? Company { get; set; }
    public virtual ICollection<AuthApi.Domain.Entities.Auth.RefreshToken> RefreshTokens { get; set; } = new List<AuthApi.Domain.Entities.Auth.RefreshToken>();
    public virtual ICollection<AuthApi.Domain.Entities.Auth.LoginHistory> LoginHistories { get; set; } = new List<AuthApi.Domain.Entities.Auth.LoginHistory>();
    public virtual ICollection<AuthApi.Domain.Entities.Auth.PasswordReset> PasswordResets { get; set; } = new List<AuthApi.Domain.Entities.Auth.PasswordReset>();
}
