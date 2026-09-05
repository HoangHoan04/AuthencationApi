using AuthApi.Domain.Common;

namespace AuthApi.Domain.Entities.Rbac;

public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Module { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
