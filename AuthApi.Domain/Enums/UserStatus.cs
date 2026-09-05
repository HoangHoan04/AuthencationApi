namespace AuthApi.Domain.Enums;

public enum UserStatus
{
    Active = 1,
    Locked = 2,
    Disabled = 3,
    Invited = 4,
    PendingVerification = 5
}
