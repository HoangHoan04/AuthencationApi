using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using AuthApi.Application;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Infrastructure;
using AuthApi.Infrastructure.Persistence;
using AuthApi.WebApi.Authorization;
using AuthApi.WebApi.Middlewares;
using AuthApi.WebApi.RateLimiting;
using AuthApi.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth Enterprise Identity API",
        Version = "v1",
        Description = "Cổng Quản lý Định danh, Xác thực Tập trung & Phân quyền Doanh nghiệp (SSO, OAuth2 PKCE, JWT RS256)"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Bearer token: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var issuer = builder.Configuration["Jwt:Issuer"] ?? "https://auth.company.com";
var audience = builder.Configuration["Jwt:Audience"] ?? "erp-ecosystem";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IRsaKeyManager>((options, rsaKeyManager) =>
    {
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            IssuerSigningKeyResolver = (_, _, _, _) => rsaKeyManager.GetValidationKeys()
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-strict", httpContext =>
    {
        var redis = httpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();
        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthRateLimit");
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.Get(
            ip,
            key => new RedisFixedWindowRateLimiter(
                redis,
                $"auth:rl:{key}",
                permitLimit: 8,
                window: TimeSpan.FromMinutes(1),
                logger));
    });
});

var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
    ?? new[]
    {
        "http://localhost:4200", "http://localhost:4201", "http://localhost:4202", "http://localhost:4203",
        "http://localhost:4300", "http://localhost:4400", "http://localhost:4500", "http://localhost:4600",
        "http://localhost:8000"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

_ = app.Services.GetRequiredService<IConnectionMultiplexer>();

await DatabaseBootstrap.InitializeDatabaseAsync(app.Services);
using (var scope = app.Services.CreateScope())
{
    var rsa = scope.ServiceProvider.GetRequiredService<IRsaKeyManager>();
    await rsa.InitializeAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("CorsPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TokenDenylistMiddleware>();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "AuthApi", timestamp = DateTime.UtcNow }));
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", service = "AuthApi", timestamp = DateTime.UtcNow }));
app.MapGet("/health/live", () => Results.Ok(new { status = "Live", service = "AuthApi" }));
app.MapGet("/health/ready", async (ApplicationDbContext db, IConnectionMultiplexer redis) =>
{
    var dbOk = await db.Database.CanConnectAsync();
    var redisOk = redis.IsConnected;
    var ready = dbOk && (redisOk || !app.Environment.IsProduction());
    return Results.Json(new
    {
        status = ready ? "Ready" : "Degraded",
        service = "AuthApi",
        db = dbOk,
        redis = redisOk,
        timestamp = DateTime.UtcNow
    }, statusCode: ready ? 200 : 503);
});

app.MapControllers();

app.Run();
