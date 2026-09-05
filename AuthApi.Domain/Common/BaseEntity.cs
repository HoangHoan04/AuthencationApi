namespace AuthApi.Domain.Common;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

public interface ITenantEntity
{
    Guid? CompanyId { get; set; }
}

public interface IHasConcurrency
{
    uint RowVersion { get; set; }
}

/// <summary>
/// Entity nghiệp vụ: audit + soft-delete + concurrency (xmin PostgreSQL).
/// CompanyId không nằm ở đây — chỉ entity đa tenant implement <see cref="ITenantEntity"/>.
/// </summary>
public abstract class BaseEntity : IAuditableEntity, ISoftDelete, IHasConcurrency
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public uint RowVersion { get; set; }
}

/// <summary>
/// Bản ghi bất biến (login history, audit, denylist). Không soft-delete, không update.
/// </summary>
public abstract class ImmutableLogEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
