using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Auth;

public class MfaDevice : BaseEntity
{
    public Guid UserId { get; set; }
    public MfaMethod Method { get; set; } = MfaMethod.Totp;
    public string Name { get; set; } = "Authenticator";
    public string SecretEncrypted { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    public virtual User? User { get; set; }
}
