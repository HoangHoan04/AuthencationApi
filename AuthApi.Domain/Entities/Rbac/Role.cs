using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Users;

namespace AuthApi.Domain.Entities.Rbac;

public class Role : BaseEntity, ITenantEntity
{
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Company? Company { get; set; }
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
