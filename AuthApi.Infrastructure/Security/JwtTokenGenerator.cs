using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
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

    public string GenerateAccessToken(User user, AccessTokenClaims? claims = null)
    {
        var rsaKey = new RsaSecurityKey(_rsaKeyManager.GetSigningKey())
        {
            KeyId = _rsaKeyManager.GetKeyId()
        };

        var roles = claims?.Roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList()
                    ?? new List<string>();

        var jti = claims?.Jti ?? Guid.NewGuid().ToString("N");
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("status", user.Status.ToString()),
            new("token_use", "access")
        };

        foreach (var role in roles)
        {
            tokenClaims.Add(new Claim(ClaimTypes.Role, role));
            tokenClaims.Add(new Claim("role", role));
        }

        foreach (var permission in claims?.Permissions ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(permission))
            {
                tokenClaims.Add(new Claim("permission", permission));
            }
        }

        foreach (var app in claims?.Apps ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(app))
            {
                tokenClaims.Add(new Claim("apps", app));
            }
        }

        if (user.CompanyId.HasValue)
        {
            tokenClaims.Add(new Claim("company_id", user.CompanyId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(user.Phone))
        {
            tokenClaims.Add(new Claim("phone", user.Phone));
        }

        var issuer = _configuration["Jwt:Issuer"] ?? "https://auth.company.com";
        var audience = _configuration["Jwt:Audience"] ?? "erp-ecosystem";
        var expiryMinutes = claims?.ExpiresMinutes
            ?? (int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var min) ? min : 15);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(tokenClaims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            IssuedAt = DateTime.UtcNow,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    public string GenerateMfaTempToken(Guid userId)
    {
        var rsaKey = new RsaSecurityKey(_rsaKeyManager.GetSigningKey())
        {
            KeyId = _rsaKeyManager.GetKeyId()
        };

        var issuer = _configuration["Jwt:Issuer"] ?? "https://auth.company.com";
        var audience = _configuration["Jwt:Audience"] ?? "erp-ecosystem";
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("token_use", "mfa"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public Guid? ReadMfaTempUserId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var issuer = _configuration["Jwt:Issuer"] ?? "https://auth.company.com";
        var audience = _configuration["Jwt:Audience"] ?? "erp-ecosystem";
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = _rsaKeyManager.GetValidationKeys(),
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            if (principal.FindFirst("token_use")?.Value != "mfa")
            {
                return null;
            }

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }

    public (string Token, string TokenHash) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        return (token, hash);
    }
}
