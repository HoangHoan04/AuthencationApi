namespace AuthApi.Application.DTOs.EcosystemApps;

public class EcosystemAppDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public List<string> RedirectUrls { get; set; } = new();
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "appstore";
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateAppRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? Namespace { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public List<string>? RedirectUrls { get; set; }
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

public class UpdateAppRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? Namespace { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public List<string>? RedirectUrls { get; set; }
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}
