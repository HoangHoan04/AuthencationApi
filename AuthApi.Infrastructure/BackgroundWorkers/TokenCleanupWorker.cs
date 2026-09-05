using AuthApi.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.BackgroundWorkers;

public class TokenCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenCleanupWorker> _logger;

    public TokenCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
                var expired = await db.RefreshTokens
                    .Where(t => t.ExpiresAt < cutoff || (t.RevokedAt != null && t.RevokedAt < cutoff))
                    .ToListAsync(stoppingToken);
                if (expired.Count > 0)
                {
                    db.RefreshTokens.RemoveRange(expired);
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Cleaned {Count} expired refresh tokens.", expired.Count);
                }

                var staleCodes = await db.AuthorizationCodes
                    .Where(c => c.ExpiresAt < DateTimeOffset.UtcNow.AddDays(-1))
                    .ToListAsync(stoppingToken);
                if (staleCodes.Count > 0)
                {
                    db.AuthorizationCodes.RemoveRange(staleCodes);
                    await db.SaveChangesAsync(stoppingToken);
                }

                var staleDeny = await db.TokenDenylists
                    .Where(d => d.ExpiresAt < DateTimeOffset.UtcNow)
                    .ToListAsync(stoppingToken);
                if (staleDeny.Count > 0)
                {
                    db.TokenDenylists.RemoveRange(staleDeny);
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token cleanup failed.");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
