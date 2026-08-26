using AuthApi.Application.DTOs.Administrative;
using AuthApi.Domain.Entities.Administrative;
using AuthApi.Domain.Enums;

namespace AuthApi.Application.Mappings;

public static class ProvinceMapper
{
    public static ProvinceDto ToDto(Province province)
    {
        return new ProvinceDto
        {
            Id = province.Id,
            Code = province.Code,
            Name = province.Name,
            FullName = province.FullName,
            DivisionType = province.DivisionType,
            DivisionTypeName = province.DivisionType.ToDisplayName(),
            AdministrativeRegion = province.AdministrativeRegion,
            SortOrder = province.SortOrder,
            IsActive = province.IsActive,
            WardCount = province.Wards?.Count(w => !w.IsDeleted) ?? 0,
            CreatedAt = province.CreatedAt
        };
    }
}
