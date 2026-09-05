using AuthApi.Domain.Common;

namespace AuthApi.Domain.Entities.Auth;

/// <summary>JTI access token bị thu hồi trước khi hết hạn.</summary>
public class TokenDenylist : ImmutableLogEntity
{
    public string Jti { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Reason { get; set; }
}
