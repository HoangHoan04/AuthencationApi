using AuthApi.Domain.Enums;

namespace AuthApi.Application.DTOs.Security;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsCurrent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsActive => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;
    public string? UserEmail { get; set; }
}

public class LoginHistoryDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string EmailAttempted { get; set; } = string.Empty;
    public LoginEventType EventType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class SecurityKeyDto
{
    public string KeyId { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "RS256";
    public string Use { get; set; } = "sig";
    public string Modulus { get; set; } = string.Empty;
    public string Exponent { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string Status { get; set; } = "Active";
    public DateTimeOffset? RotatedAt { get; set; }
}
