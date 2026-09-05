using AuthApi.Application.Common.Models;
using AuthApi.Domain.Entities.Users;

namespace AuthApi.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user, AccessTokenClaims? claims = null);
    (string Token, string TokenHash) GenerateRefreshToken();
    string GenerateMfaTempToken(Guid userId);
    Guid? ReadMfaTempUserId(string token);
}
