using AuthApi.Domain.Enums;

namespace AuthApi.Application.DTOs.Administrative;

public class ProvinceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public ProvinceDivisionType DivisionType { get; set; } = ProvinceDivisionType.Province;
    public string? DivisionTypeName { get; set; }
    public string? AdministrativeRegion { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int WardCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class WardDto
{
    public Guid Id { get; set; }
    public Guid ProvinceId { get; set; }
    public string ProvinceCode { get; set; } = string.Empty;
    public string? ProvinceName { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public WardDivisionType DivisionType { get; set; } = WardDivisionType.Commune;
    public string? DivisionTypeName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AdministrativeTreeNodeDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsLeaf { get; set; } = false;
    public List<AdministrativeTreeNodeDto>? Children { get; set; }
}

public class CreateProvinceRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public ProvinceDivisionType DivisionType { get; set; } = ProvinceDivisionType.Province;
    public string? AdministrativeRegion { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateProvinceRequest
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public ProvinceDivisionType? DivisionType { get; set; }
    public string? AdministrativeRegion { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class CreateWardRequest
{
    public Guid ProvinceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public WardDivisionType DivisionType { get; set; } = WardDivisionType.Commune;
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateWardRequest
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public WardDivisionType? DivisionType { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
