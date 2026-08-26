using AuthApi.Domain.Common;

namespace AuthApi.Domain.Entities.EcosystemApps;

public class EcosystemApp : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? Namespace { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUrlsJson { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "appstore";
    public string Color { get; set; } = "linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)";
    public string Category { get; set; } = "Hệ thống ERP";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}
