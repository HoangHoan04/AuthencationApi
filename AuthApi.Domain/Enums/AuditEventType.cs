namespace AuthApi.Domain.Enums;

public enum AuditEventType
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    StatusChanged = 4,
    SecretRotated = 5,
    RoleAssigned = 6,
    RoleRevoked = 7,
    PermissionChanged = 8,
    MfaEnabled = 9,
    MfaDisabled = 10,
    SessionRevoked = 11,
    KeyRotated = 12
}
