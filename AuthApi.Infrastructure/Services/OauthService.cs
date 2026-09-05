using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Application.DTOs.Auth;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Rbac;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AuthApi.Infrastructure.Services;

public interface IOauthService
{
    Task<string> CompleteAuthorizeAsync(Guid userId, OAuthAuthorizeCompleteRequest request);
    Task<object> IssueTokenAsync(OAuthTokenRequest request, string? ip, string? userAgent);
    Task<object> GetUserInfoAsync(Guid userId);
}

public class OauthService : IOauthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IUserAccessService _access;
    private readonly IAuthService _auth;

    public OauthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwt,
        IUserAccessService access,
        IAuthService auth)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _access = access;
        _auth = auth;
    }

    public async Task<string> CompleteAuthorizeAsync(Guid userId, OAuthAuthorizeCompleteRequest request)
    {
        EcosystemApp app = await FindAppAsync(request.ClientId);
        string redirect = request.RedirectUri.Trim();
        EnsureRedirectAllowed(app, redirect);

        if (app.RequirePkce && string.IsNullOrWhiteSpace(request.CodeChallenge))
        {
            throw new InvalidOperationException("PKCE code_challenge is required for this client.");
        }

        bool allowed = await _context.UserApps.AnyAsync(ua =>
            ua.UserId == userId && ua.AppId == app.Id && ua.IsActive && ua.RevokedAt == null);
        bool isAdmin = await _context.UserRoles.AnyAsync(ur =>
            ur.UserId == userId && (ur.Role!.Code == "SuperAdmin" || ur.Role!.Code == "Admin"));
        if (!allowed && !isAdmin)
        {
            throw new UnauthorizedAccessException("User chưa được gán vào ứng dụng này.");
        }

        string rawCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _ = _context.AuthorizationCodes.Add(new AuthorizationCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AppId = app.Id,
            CodeHash = Sha256(rawCode),
            RedirectUri = redirect,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod ?? "S256",
            Scope = request.Scope,
            Nonce = request.Nonce,
            State = request.State,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            CreatedAt = DateTimeOffset.UtcNow
        });
        _ = await _context.SaveChangesAsync();

        string separator = redirect.Contains('?') ? "&" : "?";
        string location = $"{redirect}{separator}code={rawCode}";
        if (!string.IsNullOrWhiteSpace(request.State))
        {
            location += $"&state={Uri.EscapeDataString(request.State)}";
        }

        return location;
    }

    public async Task<object> IssueTokenAsync(OAuthTokenRequest request, string? ip, string? userAgent)
    {
        string grant = (request.GrantType ?? "").Trim().ToLowerInvariant();
        return grant switch
        {
            "authorization_code" => await ExchangeCodeAsync(request, ip, userAgent),
            "refresh_token" => await RefreshGrantAsync(request, ip, userAgent),
            "client_credentials" => await ClientCredentialsAsync(request),
            _ => throw new InvalidOperationException("grant_type không được hỗ trợ.")
        };
    }

    private async Task<object> RefreshGrantAsync(OAuthTokenRequest request, string? ip, string? userAgent)
    {
        AuthResponse session = await _auth.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = request.RefreshToken ?? string.Empty },
            ip,
            userAgent);

        return new
        {
            access_token = session.AccessToken,
            refresh_token = session.RefreshToken,
            token_type = "Bearer",
            expires_in = session.ExpiresIn
        };
    }

    public async Task<object> GetUserInfoAsync(Guid userId)
    {
        User user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");
        UserAccessSnapshot access = await _access.GetAsync(userId);
        return new
        {
            sub = user.Id,
            email = user.Email,
            name = user.FullName,
            company_id = user.CompanyId,
            roles = access.Roles,
            permissions = access.Permissions,
            apps = access.Apps
        };
    }

    private async Task<object> ExchangeCodeAsync(OAuthTokenRequest request, string? ip, string? userAgent)
    {
        EcosystemApp app = await FindAppAsync(request.ClientId ?? "");
        string code = request.Code?.Trim() ?? "";
        AuthorizationCode? authCode = await _context.AuthorizationCodes
            .Include(c => c.User).ThenInclude(u => u!.Company)
            .FirstOrDefaultAsync(c => c.CodeHash == Sha256(code) && c.AppId == app.Id);

        if (authCode?.User == null || authCode.ConsumedAt != null || authCode.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Authorization code không hợp lệ.");
        }

        if (!string.Equals(authCode.RedirectUri, request.RedirectUri?.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("redirect_uri không khớp.");
        }

        if (app.RequirePkce)
        {
            if (string.IsNullOrWhiteSpace(request.CodeVerifier) || string.IsNullOrWhiteSpace(authCode.CodeChallenge))
            {
                throw new InvalidOperationException("PKCE code_verifier is required.");
            }

            string computed = S256(request.CodeVerifier);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(computed),
                    Encoding.ASCII.GetBytes(authCode.CodeChallenge)))
            {
                throw new UnauthorizedAccessException("PKCE verification failed.");
            }
        }

        authCode.ConsumedAt = DateTimeOffset.UtcNow;

        UserAccessSnapshot access = await _access.GetAsync(authCode.UserId);
        string accessToken = _jwt.GenerateAccessToken(authCode.User, new AccessTokenClaims
        {
            Roles = access.Roles,
            Permissions = access.Permissions,
            Apps = access.Apps
        });
        (string? refresh, string? refreshHash) = _jwt.GenerateRefreshToken();
        _ = _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = authCode.UserId,
            AppId = app.Id,
            CompanyId = authCode.User.CompanyId,
            TokenHash = refreshHash,
            FamilyId = Guid.NewGuid(),
            IpAddress = ip,
            UserAgent = userAgent,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(app.RefreshTokenTtlDays <= 0 ? 7 : app.RefreshTokenTtlDays),
            CreatedAt = DateTimeOffset.UtcNow
        });
        _ = await _context.SaveChangesAsync();

        return new
        {
            access_token = accessToken,
            refresh_token = refresh,
            token_type = "Bearer",
            expires_in = (app.AccessTokenTtlMinutes <= 0 ? 15 : app.AccessTokenTtlMinutes) * 60,
            id_token = accessToken,
            scope = authCode.Scope ?? "openid profile"
        };
    }

    private async Task<object> ClientCredentialsAsync(OAuthTokenRequest request)
    {
        EcosystemApp app = await FindAppAsync(request.ClientId ?? "");
        if (app.AppType != AppType.MachineToMachine && app.RequirePkce)
        {
            throw new UnauthorizedAccessException("Client này không dùng client_credentials.");
        }

        string secret = request.ClientSecret?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(app.ClientSecretHash) ||
            !_passwordHasher.VerifyPassword(secret, app.ClientSecretHash))
        {
            List<AuthClientSecret> rotated = await _context.AuthClientSecrets
                .Where(s => s.AppId == app.Id && s.IsActive && s.RevokedAt == null)
                .ToListAsync();
            if (!rotated.Any(s => _passwordHasher.VerifyPassword(secret, s.SecretHash)))
            {
                throw new UnauthorizedAccessException("client_secret không hợp lệ.");
            }
        }

        User? machine = await _context.Users.FirstOrDefaultAsync(u => u.Email == $"{app.Code}@machine.local");
        if (machine == null)
        {
            machine = new Domain.Entities.Users.User
            {
                Id = Guid.NewGuid(),
                Email = $"{app.Code}@machine.local",
                FullName = $"{app.Name} service",
                UserType = UserType.Machine,
                PasswordHash = _passwordHasher.HashPassword(Guid.NewGuid().ToString("N") + "!A"),
                Status = Domain.Enums.UserStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _ = _context.Users.Add(machine);
            _ = await _context.SaveChangesAsync();

            Role? operatorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "Operator");
            if (operatorRole != null)
            {
                _ = _context.UserRoles.Add(new Domain.Entities.Rbac.UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = machine.Id,
                    RoleId = operatorRole.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                _ = await _context.SaveChangesAsync();
            }
        }

        UserAccessSnapshot access = await _access.GetAsync(machine.Id);
        string token = _jwt.GenerateAccessToken(machine, new AccessTokenClaims
        {
            Roles = access.Roles.Count == 0 ? new[] { "Operator" } : access.Roles,
            Permissions = access.Permissions,
            Apps = new[] { app.Code },
            ExpiresMinutes = app.AccessTokenTtlMinutes <= 0 ? 15 : app.AccessTokenTtlMinutes
        });

        return new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = (app.AccessTokenTtlMinutes <= 0 ? 15 : app.AccessTokenTtlMinutes) * 60,
            scope = request.Scope ?? "client"
        };
    }

    private async Task<Domain.Entities.EcosystemApps.EcosystemApp> FindAppAsync(string clientId)
    {
        string id = clientId.Trim();
        EcosystemApp? app = await _context.EcosystemApps.FirstOrDefaultAsync(a => a.ClientId == id || a.Code == id.ToLowerInvariant());
        return app == null || !app.IsActive ? throw new InvalidOperationException("client_id không hợp lệ.") : app;
    }

    private static void EnsureRedirectAllowed(Domain.Entities.EcosystemApps.EcosystemApp app, string redirect)
    {
        var allowed = new List<string>();
        if (!string.IsNullOrWhiteSpace(app.RedirectUrlsJson))
        {
            try
            {
                allowed = JsonSerializer.Deserialize<List<string>>(app.RedirectUrlsJson) ?? [];
            }
            catch
            {
                allowed = [];
            }
        }

        if (allowed.Count == 0 && !string.IsNullOrWhiteSpace(app.Url))
        {
            allowed.Add(app.Url);
        }

        if (!allowed.Any(u => string.Equals(u.TrimEnd('/'), redirect.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("redirect_uri không nằm trong whitelist.");
        }
    }

    private static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string S256(string verifier)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
