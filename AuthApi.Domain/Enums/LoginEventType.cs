namespace AuthApi.Domain.Enums;

public enum LoginEventType
{
    LoginSuccess = 1,
    LoginFailed = 2,
    Logout = 3,
    TokenRefreshed = 4,
    PasswordResetRequested = 5,
    PasswordChanged = 6,
    AccountLocked = 7
}
