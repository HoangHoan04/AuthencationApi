using AuthApi.Domain.Entities.Users;

namespace AuthApi.Domain.Entities.Auth;

public class PasswordReset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string? OtpHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public string? RequestIp { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual User? User { get; set; }
}
