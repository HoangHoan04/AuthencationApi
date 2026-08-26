using System.Text.Json;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.EcosystemApps;
using AuthApi.Application.Mappings;
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
}

public class EcosystemAppService : IEcosystemAppService
{
    private readonly IApplicationDbContext _context;

    public EcosystemAppService(IApplicationDbContext context)
    {
        _context = context;
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

        var app = new EcosystemApp
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            ServiceName = string.IsNullOrWhiteSpace(request.ServiceName) ? $"{code}-service" : request.ServiceName.Trim(),
            Namespace = string.IsNullOrWhiteSpace(request.Namespace) ? $"erp.{code}" : request.Namespace.Trim(),
            ClientId = string.IsNullOrWhiteSpace(request.ClientId) ? $"{code}-app" : request.ClientId.Trim(),
            ClientSecret = string.IsNullOrWhiteSpace(request.ClientSecret) ? $"sec_{Guid.NewGuid().ToString("N")}" : request.ClientSecret.Trim(),
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
        await _context.SaveChangesAsync();

        return EcosystemAppMapper.ToDto(app);
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
        if (!string.IsNullOrWhiteSpace(request.ClientSecret)) app.ClientSecret = request.ClientSecret.Trim();
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
}
