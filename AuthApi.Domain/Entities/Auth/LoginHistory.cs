using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Auth;

public class LoginHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string EmailAttempted { get; set; } = string.Empty;
    public LoginEventType EventType { get; set; } = LoginEventType.LoginSuccess;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual User? User { get; set; }
}
