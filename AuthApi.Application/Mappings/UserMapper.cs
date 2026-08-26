using AuthApi.Application.DTOs.Users;
using AuthApi.Domain.Entities.Users;

namespace AuthApi.Application.Mappings;

public static class UserMapper
{
    public static UserProfileDto ToDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            CompanyId = user.CompanyId,
            CompanyCode = user.Company?.Code,
            CompanyName = user.Company?.Name,
            Email = user.Email,
            Phone = user.Phone,
            FullName = user.FullName,
            Role = user.Role,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status
        };
    }
}
