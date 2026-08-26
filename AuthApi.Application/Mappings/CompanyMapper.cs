using AuthApi.Application.DTOs.Companies;
using AuthApi.Domain.Entities.Companies;

namespace AuthApi.Application.Mappings;

public static class CompanyMapper
{
    public static CompanyDto ToDto(Company company)
    {
        return new CompanyDto
        {
            Id = company.Id,
            Code = company.Code,
            Name = company.Name,
            TaxCode = company.TaxCode,
            Phone = company.Phone,
            Email = company.Email,
            Website = company.Website,
            Logo = company.Logo,
            IsActive = company.IsActive,
            UserCount = company.Users?.Count(u => !u.IsDeleted) ?? 0,
            CreatedAt = company.CreatedAt
        };
    }
}
