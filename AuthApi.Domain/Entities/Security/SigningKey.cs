using AuthApi.Domain.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Security;

public class SigningKey : BaseEntity
{
    public string KeyId { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "RS256";
    public string Use { get; set; } = "sig";
    public string PrivateKeyPemEncrypted { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
    public SigningKeyStatus Status { get; set; } = SigningKeyStatus.Active;
    public DateTimeOffset? RotatedAt { get; set; }
    public DateTimeOffset? RetireAfter { get; set; }
}
