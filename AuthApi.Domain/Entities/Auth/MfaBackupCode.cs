using AuthApi.Domain.Entities.Users;

namespace AuthApi.Domain.Entities.Auth;

public class MfaBackupCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual User? User { get; set; }
}
