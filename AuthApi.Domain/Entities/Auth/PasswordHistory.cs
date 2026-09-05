using AuthApi.Domain.Entities.Users;

namespace AuthApi.Domain.Entities.Auth;

public class PasswordHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual User? User { get; set; }
}
