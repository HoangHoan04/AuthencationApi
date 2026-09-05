using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Auth;

public class LoginHistory : ImmutableLogEntity
{
    public Guid? UserId { get; set; }
    public Guid? AppId { get; set; }
    public string EmailAttempted { get; set; } = string.Empty;
    public LoginEventType EventType { get; set; } = LoginEventType.LoginSuccess;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public string? GeoCountry { get; set; }
    public string? CorrelationId { get; set; }
    public string? FailureReason { get; set; }

    public virtual User? User { get; set; }
    public virtual EcosystemApp? App { get; set; }
}
