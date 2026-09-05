using AuthApi.Domain.Enums;
using AuthApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.WebApi.Controllers;

public class LoginTrendItemDto
{
    public string Date { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}

public class RoleDistributionDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class AppDistributionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int UserCount { get; set; }
}

public class RecentLoginLogDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AuthDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalApps { get; set; }
    public int ActiveApps { get; set; }
    public int ActiveSessions { get; set; }
    public int MfaEnabledUsers { get; set; }
    public double MfaAdoptionRate { get; set; }
    public List<LoginTrendItemDto> LoginTrend { get; set; } = new();
    public List<RoleDistributionDto> RolesDistribution { get; set; } = new();
    public List<AppDistributionDto> AppsDistribution { get; set; } = new();
    public List<RecentLoginLogDto> RecentLogs { get; set; } = new();
}

[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
[Route("api/admin/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AuthDashboardStatsDto>> GetDashboardStats(CancellationToken ct = default)
    {
        var totalUsers = await _db.Users.CountAsync(ct);
        var activeUsers = await _db.Users.CountAsync(u => u.Status == UserStatus.Active, ct);
        var totalApps = await _db.EcosystemApps.CountAsync(ct);
        var activeApps = await _db.EcosystemApps.CountAsync(a => a.IsActive, ct);

        var now = DateTimeOffset.UtcNow;
        var activeSessions = await _db.RefreshTokens
            .CountAsync(t => t.RevokedAt == null && t.ExpiresAt > now, ct);

        var mfaEnabledUsers = await _db.Users.CountAsync(u => u.MfaEnabled, ct);
        var mfaRate = totalUsers > 0 ? Math.Round((double)mfaEnabledUsers * 100.0 / totalUsers, 1) : 0;

        // 14-day login trend (UTC offset 0 for PostgreSQL timestamp with time zone)
        var startOfTodayUtc = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var cutoff = startOfTodayUtc.AddDays(-14);
        var rawLogs = await _db.LoginHistories
            .Where(l => l.CreatedAt >= cutoff)
            .Select(l => new { l.CreatedAt, l.EventType })
            .ToListAsync(ct);

        var trend = new List<LoginTrendItemDto>();
        for (var i = 13; i >= 0; i--)
        {
            var targetDay = startOfTodayUtc.AddDays(-i).ToString("yyyy-MM-dd");
            var dayLogs = rawLogs.Where(l => l.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd") == targetDay).ToList();
            trend.Add(new LoginTrendItemDto
            {
                Date = targetDay,
                SuccessCount = dayLogs.Count(l => l.EventType == LoginEventType.LoginSuccess),
                FailedCount = dayLogs.Count(l => l.EventType != LoginEventType.LoginSuccess)
            });
        }

        // Roles distribution
        var roleCounts = await _db.UserRoles
            .Include(ur => ur.Role)
            .GroupBy(ur => ur.Role != null ? ur.Role.Name : "Khác")
            .Select(g => new RoleDistributionDto
            {
                Name = g.Key,
                Value = g.Count()
            })
            .ToListAsync(ct);

        // Apps distribution (assigned users count)
        var appCounts = await _db.EcosystemApps
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .Select(a => new AppDistributionDto
            {
                Code = a.Code,
                Name = a.Name,
                Color = a.Color,
                UserCount = _db.UserApps.Count(ua => ua.AppId == a.Id && ua.IsActive && ua.RevokedAt == null)
            })
            .ToListAsync(ct);

        // Recent 8 logs
        var recentLogs = await _db.LoginHistories
            .OrderByDescending(l => l.CreatedAt)
            .Take(8)
            .Select(l => new RecentLoginLogDto
            {
                Id = l.Id,
                Email = l.EmailAttempted,
                EventType = l.EventType.ToString(),
                Success = l.EventType == LoginEventType.LoginSuccess,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                FailureReason = l.FailureReason,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new AuthDashboardStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalApps = totalApps,
            ActiveApps = activeApps,
            ActiveSessions = activeSessions,
            MfaEnabledUsers = mfaEnabledUsers,
            MfaAdoptionRate = mfaRate,
            LoginTrend = trend,
            RolesDistribution = roleCounts,
            AppsDistribution = appCounts,
            RecentLogs = recentLogs
        });
    }
}
