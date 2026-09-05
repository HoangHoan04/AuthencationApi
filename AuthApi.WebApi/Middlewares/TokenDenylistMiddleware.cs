using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthApi.Application.Common.Interfaces;

namespace AuthApi.WebApi.Middlewares;

public class TokenDenylistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenDenylistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenDenylist denylist)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsAnonymousAuthPath(path))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirstValue(JwtRegisteredClaimNames.Jti)
                      ?? context.User.FindFirstValue("jti");
            if (!string.IsNullOrWhiteSpace(jti) && await denylist.IsRevokedAsync(jti, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Token đã bị thu hồi." });
                return;
            }

            var sub = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(sub, out var userId) &&
                await denylist.IsUserAccessRevokedAsync(userId, ReadIssuedAt(context.User), context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Phiên đăng nhập đã bị thu hồi." });
                return;
            }
        }

        await _next(context);
    }

    private static bool IsAnonymousAuthPath(string path)
    {
        return path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/auth/forgot-password", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/auth/reset-password", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/auth/accept-invite", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/auth/2fa/verify", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/oauth/token", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset? ReadIssuedAt(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(JwtRegisteredClaimNames.Iat)
                  ?? user.FindFirstValue("iat");
        if (long.TryParse(raw, out var seconds) && seconds > 0)
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        return null;
    }
}
