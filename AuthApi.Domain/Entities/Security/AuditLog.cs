using AuthApi.Domain.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Security;

public class AuditLog : ImmutableLogEntity
{
    public Guid? ActorUserId { get; set; }
    public Guid? CompanyId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public AuditEventType EventType { get; set; } = AuditEventType.Updated;
    public string? Summary { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
}
