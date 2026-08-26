using System.Text.Json;
using AuthApi.Application.DTOs.EcosystemApps;
using AuthApi.Domain.Entities.EcosystemApps;

namespace AuthApi.Application.Mappings;

public static class EcosystemAppMapper
{
    public static EcosystemAppDto ToDto(EcosystemApp app)
    {
        List<string> redirectUrls = new();
        if (!string.IsNullOrWhiteSpace(app.RedirectUrlsJson))
        {
            try
            {
                redirectUrls = JsonSerializer.Deserialize<List<string>>(app.RedirectUrlsJson) ?? new List<string>();
            }
            catch
            {
                redirectUrls = new List<string>();
            }
        }
        if (redirectUrls.Count == 0 && !string.IsNullOrWhiteSpace(app.Url))
        {
            redirectUrls.Add(app.Url);
        }

        return new EcosystemAppDto
        {
            Id = app.Id,
            Code = app.Code,
            Name = app.Name,
            Description = app.Description,
            ServiceName = app.ServiceName ?? $"{app.Code}-service",
            Namespace = app.Namespace ?? $"erp.{app.Code}",
            ClientId = app.ClientId ?? $"client_{app.Code}",
            ClientSecret = app.ClientSecret ?? string.Empty,
            RedirectUrls = redirectUrls,
            Url = app.Url,
            Icon = app.Icon,
            Color = app.Color,
            Category = app.Category,
            IsActive = app.IsActive,
            SortOrder = app.SortOrder,
            CreatedAt = app.CreatedAt
        };
    }
}
