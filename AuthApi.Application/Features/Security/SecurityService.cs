using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Security;
using AuthApi.Application.Mappings;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Security;

public interface ISecurityService
{
    Task<List<SessionDto>> GetSessionsAsync(Guid? userId, bool includeRevoked = false);
    Task<bool> RevokeSessionAsync(Guid sessionId, Guid? requestingUserId);
    Task<List<LoginHistoryDto>> GetLoginHistoriesAsync(int limit = 100);
}

public class SecurityService : ISecurityService
{
    private readonly IApplicationDbContext _context;

    public SecurityService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SessionDto>> GetSessionsAsync(Guid? userId, bool includeRevoked = false)
    {
        var query = _context.RefreshTokens
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(t => t.UserId == userId.Value);
        }

        if (!includeRevoked)
        {
            query = query.Where(t => t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow);
        }

        var tokens = await query.ToListAsync();
        return tokens.Select(SecurityMapper.ToDto).ToList();
    }

    public async Task<bool> RevokeSessionAsync(Guid sessionId, Guid? requestingUserId)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Id == sessionId);
        if (token == null) return false;

        token.RevokedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<LoginHistoryDto>> GetLoginHistoriesAsync(int limit = 100)
    {
        var logs = await _context.LoginHistories
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return logs.Select(SecurityMapper.ToDto).ToList();
    }
}
