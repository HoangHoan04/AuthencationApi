using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.EcosystemApps;

namespace AuthApi.Domain.Entities.Auth;

/// <summary>Lịch sử client secret đã hash — hỗ trợ rotate mà không cắt ngay client cũ.</summary>
public class AuthClientSecret : BaseEntity
{
    public Guid AppId { get; set; }
    public string SecretHash { get; set; } = string.Empty;
    public string SecretPrefix { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual EcosystemApp? App { get; set; }
}
