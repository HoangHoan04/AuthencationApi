using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.Rbac;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.EcosystemApps;

public class EcosystemApp : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? Namespace { get; set; }
    public string? ClientId { get; set; }

    /// <summary>Hash của client secret hiện hành. Không bao giờ trả plaintext.</summary>
    public string? ClientSecretHash { get; set; }

    public DateTimeOffset? SecretLastRotatedAt { get; set; }
    public string? RedirectUrlsJson { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "appstore";
    public string Color { get; set; } = "linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)";
    public string Category { get; set; } = "Hệ thống ERP";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public AppType AppType { get; set; } = AppType.Spa;
    public string? GrantTypesJson { get; set; }
    public string? ScopesJson { get; set; }
    public bool RequirePkce { get; set; } = true;
    public int AccessTokenTtlMinutes { get; set; } = 15;
    public int RefreshTokenTtlDays { get; set; } = 7;
    public string? AllowedOriginsJson { get; set; }

    public virtual ICollection<AuthClientSecret> ClientSecrets { get; set; } = new List<AuthClientSecret>();
    public virtual ICollection<UserApp> UserApps { get; set; } = new List<UserApp>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
