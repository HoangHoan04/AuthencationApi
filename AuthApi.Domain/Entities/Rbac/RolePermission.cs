using AuthApi.Domain.Common;

namespace AuthApi.Domain.Entities.Rbac;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public virtual Role? Role { get; set; }
    public virtual Permission? Permission { get; set; }
}
