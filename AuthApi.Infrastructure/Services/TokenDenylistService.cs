using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AuthApi.Infrastructure.Services;

public class TokenDenylistService : ITokenDenylist
{
    private const string JtiPrefix = "auth:deny:jti:";
    private const string UserPrefix = "auth:deny:user:";

    private readonly IApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<TokenDenylistService> _logger;

    public TokenDenylistService(
        IApplicationDbContext context,
        IConnectionMultiplexer redis,
        ILogger<TokenDenylistService> logger)
    {
        _context = context;
        _redis = redis;
        _logger = logger;
    }

    public async Task RevokeJtiAsync(string jti, Guid? userId, DateTimeOffset expiresAt, string? reason)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return;
        }

        _context.TokenDenylists.Add(new TokenDenylist
        {
            Id = Guid.NewGuid(),
            Jti = jti,
            UserId = userId,
            ExpiresAt = expiresAt,
            Reason = reason,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync();
        await SetRedisAsync($"{JtiPrefix}{jti}", "1", expiresAt);
    }

    public async Task RevokeUserAsync(Guid userId, DateTimeOffset expiresAt, string? reason)
    {
        _context.TokenDenylists.Add(new TokenDenylist
        {
            Id = Guid.NewGuid(),
            Jti = $"user:{userId:N}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            UserId = userId,
            ExpiresAt = expiresAt,
            Reason = reason,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync();
        await SetRedisAsync(
            $"{UserPrefix}{userId:N}",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            expiresAt);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return false;
        }

        if (await RedisEqualsAsync($"{JtiPrefix}{jti}", "1"))
        {
            return true;
        }

        var exists = await _context.TokenDenylists
            .AnyAsync(x => x.Jti == jti && x.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);
        if (exists)
        {
            await SetRedisAsync($"{JtiPrefix}{jti}", "1", DateTimeOffset.UtcNow.AddMinutes(15));
        }

        return exists;
    }

    public async Task<bool> IsUserAccessRevokedAsync(
        Guid userId,
        DateTimeOffset? accessTokenIssuedAt,
        CancellationToken cancellationToken = default)
    {
        if (!accessTokenIssuedAt.HasValue)
        {
            return false;
        }

        var issuedUnix = accessTokenIssuedAt.Value.ToUnixTimeSeconds();
        var redisValue = await RedisGetAsync($"{UserPrefix}{userId:N}");
        if (long.TryParse(redisValue, out var redisUnix))
        {
            return issuedUnix < ToNotBeforeUnix(redisUnix);
        }

        var row = await _context.TokenDenylists
            .Where(x => x.UserId == userId && x.ExpiresAt > DateTimeOffset.UtcNow && x.Jti.StartsWith("user:"))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null)
        {
            return false;
        }

        var notBeforeUnix = row.CreatedAt.ToUnixTimeSeconds();
        await SetRedisAsync($"{UserPrefix}{userId:N}", notBeforeUnix.ToString(), row.ExpiresAt);
        return issuedUnix < notBeforeUnix;
    }

    /// <summary>
    /// New keys store not-before (unix at revoke). Legacy keys stored "deny until" (a future unix).
    /// </summary>
    private static long ToNotBeforeUnix(long storedUnix)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return storedUnix > now ? storedUnix - 900 : storedUnix;
    }

    private async Task SetRedisAsync(string key, string value, DateTimeOffset expiresAt)
    {
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            await _redis.GetDatabase().StringSetAsync(key, value, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis denylist SET failed for {Key}", key);
        }
    }

    private async Task<bool> RedisEqualsAsync(string key, string expected)
    {
        var value = await RedisGetAsync(key);
        return string.Equals(value, expected, StringComparison.Ordinal);
    }

    private async Task<string?> RedisGetAsync(string key)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis denylist GET failed for {Key}", key);
            return null;
        }
    }
}
