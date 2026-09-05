using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Users;

namespace AuthApi.Domain.Entities.Auth;

public class AuthorizationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid AppId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? Scope { get; set; }
    public string? Nonce { get; set; }
    public string? State { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual User? User { get; set; }
    public virtual EcosystemApp? App { get; set; }
}
