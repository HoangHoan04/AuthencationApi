using AuthApi.Application.Common.Models;
using AuthApi.Application.DTOs.Users;
using AuthApi.Domain.Entities.Users;

namespace AuthApi.Application.Mappings;

public static class UserMapper
{
    public static UserProfileDto ToDto(User user)
    {
        return UserProfileFactory.From(user);
    }
}
