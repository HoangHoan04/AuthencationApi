namespace AuthApi.Application.Common.Interfaces;

public interface IPasswordPolicy
{
    void Validate(string password);
}

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task WriteAsync(
        string entityType,
        Guid? entityId,
        AuthApi.Domain.Enums.AuditEventType eventType,
        string? summary,
        object? before = null,
        object? after = null);
}

public interface IDataProtectionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public sealed class UserAccessSnapshot
{
    public List<string> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
    public List<string> Apps { get; init; } = new();
}

public interface IUserAccessService
{
    Task<UserAccessSnapshot> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface ITokenDenylist
{
    Task RevokeJtiAsync(string jti, Guid? userId, DateTimeOffset expiresAt, string? reason);
    Task RevokeUserAsync(Guid userId, DateTimeOffset expiresAt, string? reason);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
    Task<bool> IsUserAccessRevokedAsync(Guid userId, DateTimeOffset? accessTokenIssuedAt, CancellationToken cancellationToken = default);
}
