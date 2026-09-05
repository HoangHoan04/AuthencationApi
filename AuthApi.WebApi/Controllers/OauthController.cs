using System.Text.Json;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Auth;
using AuthApi.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthApi.WebApi.Controllers;

[ApiController]
public class OauthController : ControllerBase
{
    private readonly IOauthService _oauth;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;

    public OauthController(IOauthService oauth, ICurrentUserService currentUser, IConfiguration configuration)
    {
        _oauth = oauth;
        _currentUser = currentUser;
        _configuration = configuration;
    }

    [HttpGet(".well-known/openid-configuration")]
    [HttpGet("api/.well-known/openid-configuration")]
    [AllowAnonymous]
    public IActionResult Discovery()
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "https://auth.company.com";
        var apiBase = (_configuration["Auth:ApiPublicBaseUrl"] ?? issuer).TrimEnd('/');
        return Ok(new
        {
            issuer,
            authorization_endpoint = $"{apiBase}/oauth/authorize",
            token_endpoint = $"{apiBase}/oauth/token",
            userinfo_endpoint = $"{apiBase}/oauth/userinfo",
            jwks_uri = $"{apiBase}/.well-known/jwks.json",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token", "client_credentials" },
            code_challenge_methods_supported = new[] { "S256" },
            scopes_supported = new[] { "openid", "profile", "email" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "none" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" }
        });
    }

    [HttpGet("oauth/authorize")]
    [HttpGet("api/oauth/authorize")]
    [AllowAnonymous]
    public IActionResult Authorize()
    {
        var adminBase = (_configuration["Auth:PublicBaseUrl"] ?? "http://localhost:4300").TrimEnd('/');
        return Redirect($"{adminBase}/auth/sso{Request.QueryString}");
    }

    [Authorize]
    [HttpPost("oauth/authorize/complete")]
    [HttpPost("api/oauth/authorize/complete")]
    public async Task<IActionResult> Complete([FromBody] OAuthAuthorizeCompleteRequest request)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var location = await _oauth.CompleteAuthorizeAsync(_currentUser.UserId.Value, request);
            return Ok(new { location });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    [HttpPost("oauth/token")]
    [HttpPost("api/oauth/token")]
    public async Task<IActionResult> Token()
    {
        var request = await BindTokenRequestAsync();
        var result = await _oauth.IssueTokenAsync(request, _currentUser.IpAddress, _currentUser.UserAgent);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("oauth/userinfo")]
    [HttpGet("api/oauth/userinfo")]
    public async Task<IActionResult> UserInfo()
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        return Ok(await _oauth.GetUserInfoAsync(_currentUser.UserId.Value));
    }

    private async Task<OAuthTokenRequest> BindTokenRequestAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            return new OAuthTokenRequest
            {
                GrantType = form["grant_type"].ToString(),
                Code = form["code"],
                RedirectUri = form["redirect_uri"],
                ClientId = form["client_id"],
                ClientSecret = form["client_secret"],
                CodeVerifier = form["code_verifier"],
                RefreshToken = form["refresh_token"],
                Scope = form["scope"]
            };
        }

        return await JsonSerializer.DeserializeAsync<OAuthTokenRequest>(
                   Request.Body,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new OAuthTokenRequest();
    }
}
