using System.Security.Cryptography;
using System.Text.Json;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.EcosystemApps;
using AuthApi.Application.Mappings;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.EcosystemApps;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.EcosystemApps;

public interface IEcosystemAppService
{
    Task<List<EcosystemAppDto>> GetAppsAsync();
    Task<EcosystemAppDto?> GetByClientIdAsync(string clientId);
    Task<EcosystemAppDto> CreateAppAsync(CreateAppRequest request);
    Task<EcosystemAppDto> UpdateAppAsync(Guid id, UpdateAppRequest request);
    Task<bool> DeleteAppAsync(Guid id);
    Task<EcosystemAppDto> RotateSecretAsync(Guid id);
}

public class EcosystemAppService : IEcosystemAppService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public EcosystemAppService(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<EcosystemAppDto>> GetAppsAsync()
    {
        var apps = await _context.EcosystemApps
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Name)
            .ToListAsync();

        return apps.Select(EcosystemAppMapper.ToDto).ToList();
    }

    public async Task<EcosystemAppDto?> GetByClientIdAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var trimmed = clientId.Trim();
        var app = await _context.EcosystemApps
            .FirstOrDefaultAsync(a => a.ClientId == trimmed || a.Code == trimmed.ToLowerInvariant());

        return app != null ? EcosystemAppMapper.ToDto(app) : null;
    }

    public async Task<EcosystemAppDto> CreateAppAsync(CreateAppRequest request)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        var exists = await _context.EcosystemApps.AnyAsync(a => a.Code == code);
        if (exists)
        {
            throw new InvalidOperationException("Mã ứng dụng đã tồn tại trên hệ thống.");
        }

        var redirectList = request.RedirectUrls ?? new List<string>();
        if (redirectList.Count == 0 && !string.IsNullOrWhiteSpace(request.Url))
        {
            redirectList.Add(request.Url.Trim());
        }

        var plaintextSecret = string.IsNullOrWhiteSpace(request.ClientSecret)
            ? GenerateSecret()
            : request.ClientSecret.Trim();

        var app = new EcosystemApp
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            ServiceName = string.IsNullOrWhiteSpace(request.ServiceName) ? $"{code}-service" : request.ServiceName.Trim(),
            Namespace = string.IsNullOrWhiteSpace(request.Namespace) ? $"erp.{code}" : request.Namespace.Trim(),
            ClientId = string.IsNullOrWhiteSpace(request.ClientId) ? $"{code}-app" : request.ClientId.Trim(),
            ClientSecretHash = _passwordHasher.HashPassword(plaintextSecret),
            SecretLastRotatedAt = DateTimeOffset.UtcNow,
            AppType = AuthApi.Domain.Enums.AppType.Spa,
            RequirePkce = request.RequirePkce ?? true,
            RedirectUrlsJson = JsonSerializer.Serialize(redirectList),
            Url = !string.IsNullOrWhiteSpace(request.Url) ? request.Url.Trim() : (redirectList.FirstOrDefault() ?? "http://localhost:4200"),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? "appstore" : request.Icon.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? "linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)" : request.Color.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Hệ thống ERP" : request.Category.Trim(),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.EcosystemApps.Add(app);
        AddSecretHistory(app.Id, plaintextSecret);
        await _context.SaveChangesAsync();

        var dto = EcosystemAppMapper.ToDto(app);
        dto.PlaintextSecret = plaintextSecret;
        return dto;
    }

    public async Task<EcosystemAppDto> UpdateAppAsync(Guid id, UpdateAppRequest request)
    {
        var app = await _context.EcosystemApps.FirstOrDefaultAsync(a => a.Id == id);
        if (app == null)
        {
            throw new KeyNotFoundException("Không tìm thấy ứng dụng.");
        }

        var redirectList = request.RedirectUrls ?? new List<string>();
        if (redirectList.Count == 0 && !string.IsNullOrWhiteSpace(request.Url))
        {
            redirectList.Add(request.Url.Trim());
        }

        app.Name = request.Name.Trim();
        app.Description = request.Description.Trim();
        app.ServiceName = string.IsNullOrWhiteSpace(request.ServiceName) ? $"{app.Code}-service" : request.ServiceName.Trim();
        app.Namespace = string.IsNullOrWhiteSpace(request.Namespace) ? $"erp.{app.Code}" : request.Namespace.Trim();
        if (!string.IsNullOrWhiteSpace(request.ClientId)) app.ClientId = request.ClientId.Trim();
        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            app.ClientSecretHash = _passwordHasher.HashPassword(request.ClientSecret.Trim());
            app.SecretLastRotatedAt = DateTimeOffset.UtcNow;
            AddSecretHistory(app.Id, request.ClientSecret.Trim());
        }
        if (request.RequirePkce.HasValue)
        {
            app.RequirePkce = request.RequirePkce.Value;
        }
        app.RedirectUrlsJson = JsonSerializer.Serialize(redirectList);
        app.Url = !string.IsNullOrWhiteSpace(request.Url) ? request.Url.Trim() : (redirectList.FirstOrDefault() ?? app.Url);
        app.Icon = string.IsNullOrWhiteSpace(request.Icon) ? "appstore" : request.Icon.Trim();
        app.Color = string.IsNullOrWhiteSpace(request.Color) ? "linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)" : request.Color.Trim();
        app.Category = string.IsNullOrWhiteSpace(request.Category) ? "Hệ thống ERP" : request.Category.Trim();
        app.IsActive = request.IsActive;
        app.SortOrder = request.SortOrder;

        await _context.SaveChangesAsync();

        return EcosystemAppMapper.ToDto(app);
    }

    public async Task<bool> DeleteAppAsync(Guid id)
    {
        var app = await _context.EcosystemApps.FirstOrDefaultAsync(a => a.Id == id);
        if (app != null)
        {
            app.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<EcosystemAppDto> RotateSecretAsync(Guid id)
    {
        var app = await _context.EcosystemApps.FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy ứng dụng.");

        var previous = await _context.AuthClientSecrets
            .Where(s => s.AppId == id && s.IsActive && s.RevokedAt == null)
            .ToListAsync();
        foreach (var secret in previous)
        {
            secret.ExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
        }

        var plaintext = GenerateSecret();
        app.ClientSecretHash = _passwordHasher.HashPassword(plaintext);
        app.SecretLastRotatedAt = DateTimeOffset.UtcNow;
        AddSecretHistory(app.Id, plaintext);
        await _context.SaveChangesAsync();

        var dto = EcosystemAppMapper.ToDto(app);
        dto.PlaintextSecret = plaintext;
        return dto;
    }

    private void AddSecretHistory(Guid appId, string plaintext)
    {
        _context.AuthClientSecrets.Add(new AuthClientSecret
        {
            Id = Guid.NewGuid(),
            AppId = appId,
            SecretHash = _passwordHasher.HashPassword(plaintext),
            SecretPrefix = plaintext.Length <= 8 ? plaintext[..Math.Min(4, plaintext.Length)] : plaintext[..8],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static string GenerateSecret() => $"sec_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}";
}
