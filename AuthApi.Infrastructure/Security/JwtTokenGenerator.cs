using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IRsaKeyManager _rsaKeyManager;
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IRsaKeyManager rsaKeyManager, IConfiguration configuration)
    {
        _rsaKeyManager = rsaKeyManager;
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        var rsaKey = new RsaSecurityKey(_rsaKeyManager.GetSigningKey())
        {
            KeyId = _rsaKeyManager.GetKeyId()
        };

        var signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("status", user.Status.ToString())
        };

        if (user.CompanyId.HasValue)
        {
            claims.Add(new Claim("company_id", user.CompanyId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(user.Phone))
        {
            claims.Add(new Claim("phone", user.Phone));
        }

        var issuer = _configuration["Jwt:Issuer"] ?? "https://auth.company.com";
        var audience = _configuration["Jwt:Audience"] ?? "erp-ecosystem";
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var min) ? min : 15;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public (string Token, string TokenHash) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Hash refresh token with SHA256 before storing
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        var tokenHash = Convert.ToHexString(hashBytes);

        return (token, tokenHash);
    }
}
