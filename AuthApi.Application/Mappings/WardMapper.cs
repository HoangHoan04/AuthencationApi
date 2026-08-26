using AuthApi.Application.DTOs.Administrative;
using AuthApi.Domain.Entities.Administrative;
using AuthApi.Domain.Enums;

namespace AuthApi.Application.Mappings;

public static class WardMapper
{
    public static WardDto ToDto(Ward ward)
    {
        return new WardDto
        {
            Id = ward.Id,
            ProvinceId = ward.ProvinceId,
            ProvinceCode = ward.ProvinceCode,
            ProvinceName = ward.Province?.Name,
            Code = ward.Code,
            Name = ward.Name,
            FullName = ward.FullName,
            DivisionType = ward.DivisionType,
            DivisionTypeName = ward.DivisionType.ToDisplayName(),
            SortOrder = ward.SortOrder,
            IsActive = ward.IsActive,
            CreatedAt = ward.CreatedAt
        };
    }
}
