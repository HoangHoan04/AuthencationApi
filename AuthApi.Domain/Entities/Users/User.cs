using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Entities.Rbac;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Users;

public class User : BaseEntity, ITenantEntity
{
    public Guid? CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public UserType UserType { get; set; } = UserType.Human;
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }

    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public DateTimeOffset? PhoneVerifiedAt { get; set; }
    public bool MfaEnabled { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? PasswordChangedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public string? Locale { get; set; }
    public string? Timezone { get; set; }

    public virtual Company? Company { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();
    public virtual ICollection<PasswordReset> PasswordResets { get; set; } = new List<PasswordReset>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserApp> UserApps { get; set; } = new List<UserApp>();
    public virtual ICollection<MfaDevice> MfaDevices { get; set; } = new List<MfaDevice>();
    public virtual ICollection<MfaBackupCode> MfaBackupCodes { get; set; } = new List<MfaBackupCode>();
    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
}
