using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Users;

namespace AuthApi.Domain.Entities.Rbac;

/// <summary>Gán role cho user, có thể theo app và theo tenant.</summary>
public class UserRole : BaseEntity, ITenantEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? AppId { get; set; }
    public Guid? CompanyId { get; set; }

    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
    public virtual EcosystemApp? App { get; set; }
    public virtual Company? Company { get; set; }
}
