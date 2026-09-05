using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Users;

namespace AuthApi.Domain.Entities.Users;

/// <summary>User được phép vào app nào trong hệ sinh thái.</summary>
public class UserApp : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AppId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? GrantedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public virtual User? User { get; set; }
    public virtual EcosystemApp? App { get; set; }
}
