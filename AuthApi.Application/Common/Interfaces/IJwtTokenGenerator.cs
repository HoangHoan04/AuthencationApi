using AuthApi.Domain.Entities.Users;

namespace AuthApi.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    (string Token, string TokenHash) GenerateRefreshToken();
}
