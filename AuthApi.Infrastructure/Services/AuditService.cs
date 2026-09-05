using System.Text.Json;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Security;
using AuthApi.Domain.Enums;

namespace AuthApi.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AuditService(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task WriteAsync(
        string entityType,
        Guid? entityId,
        AuditEventType eventType,
        string? summary,
        object? before = null,
        object? after = null)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = _currentUser.UserId,
            CompanyId = _currentUser.CompanyId,
            EntityType = entityType,
            EntityId = entityId,
            EventType = eventType,
            Summary = summary,
            BeforeJson = before == null ? null : JsonSerializer.Serialize(before),
            AfterJson = after == null ? null : JsonSerializer.Serialize(after),
            IpAddress = _currentUser.IpAddress,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
