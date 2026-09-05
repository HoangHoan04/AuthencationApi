using System.Threading.RateLimiting;
using StackExchange.Redis;

namespace AuthApi.WebApi.RateLimiting;

public sealed class RedisFixedWindowRateLimiter : RateLimiter
{
    private const string IncrExpireLua = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
          redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        return current
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly string _key;
    private readonly int _permitLimit;
    private readonly TimeSpan _window;
    private readonly ILogger _logger;

    public RedisFixedWindowRateLimiter(
        IConnectionMultiplexer redis,
        string key,
        int permitLimit,
        TimeSpan window,
        ILogger logger)
    {
        _redis = redis;
        _key = key;
        _permitLimit = permitLimit;
        _window = window;
        _logger = logger;
    }

    public override TimeSpan? IdleDuration => TimeSpan.Zero;

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        try
        {
            var windowSeconds = Math.Max(1, (long)_window.TotalSeconds);
            var windowId = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / windowSeconds;
            var redisKey = $"{_key}:{windowId}";
            var db = _redis.GetDatabase();
            var count = (long)db.ScriptEvaluate(
                IncrExpireLua,
                new RedisKey[] { redisKey },
                new RedisValue[] { windowSeconds });

            return new RedisLease(count <= _permitLimit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis rate limit failed for {Key}; allowing request.", _key);
            return new RedisLease(true);
        }
    }

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(AttemptAcquireCore(permitCount));
    }

    private sealed class RedisLease : RateLimitLease
    {
        public RedisLease(bool isAcquired)
        {
            IsAcquired = isAcquired;
        }

        public override bool IsAcquired { get; }

        public override IEnumerable<string> MetadataNames => Array.Empty<string>();

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
