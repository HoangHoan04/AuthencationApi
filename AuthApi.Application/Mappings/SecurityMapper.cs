using AuthApi.Application.DTOs.Security;
using AuthApi.Domain.Entities.Auth;

namespace AuthApi.Application.Mappings;

public static class SecurityMapper
{
    public static LoginHistoryDto ToDto(LoginHistory log)
    {
        return new LoginHistoryDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserName = log.User?.FullName,
            EmailAttempted = log.EmailAttempted,
            EventType = log.EventType,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Location = log.Location,
            FailureReason = log.FailureReason,
            CreatedAt = log.CreatedAt
        };
    }

    public static SessionDto ToDto(RefreshToken token)
    {
        return new SessionDto
        {
            Id = token.Id,
            FamilyId = token.FamilyId,
            DeviceName = token.DeviceName,
            IpAddress = token.IpAddress,
            UserAgent = token.UserAgent,
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt,
            IsRevoked = token.RevokedAt != null,
            UserEmail = token.User?.Email
        };
    }
}
