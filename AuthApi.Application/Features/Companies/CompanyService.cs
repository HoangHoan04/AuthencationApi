using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Companies;
using AuthApi.Application.Mappings;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Companies;

public interface ICompanyService
{
    Task<List<CompanyDto>> GetCompaniesAsync();
    Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request);
    Task<CompanyDto> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request);
    Task<bool> DeleteCompanyAsync(Guid id);
}

public class CompanyService : ICompanyService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public CompanyService(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<CompanyDto>> GetCompaniesAsync()
    {
        var companies = await _context.Companies
            .Include(c => c.Users)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return companies.Select(CompanyMapper.ToDto).ToList();
    }

    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _context.Companies.AnyAsync(c => c.Code == code);
        if (exists)
        {
            throw new InvalidOperationException("Mã đối tác đã tồn tại trên hệ thống.");
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            TaxCode = request.TaxCode?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Website = request.Website?.Trim(),
            Logo = request.Logo?.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Companies.Add(company);

        if (!string.IsNullOrWhiteSpace(request.AdminUsername) && !string.IsNullOrWhiteSpace(request.Password))
        {
            var adminEmail = request.AdminUsername.Trim().ToLowerInvariant();
            var userExists = await _context.Users.AnyAsync(u => u.Email == adminEmail);
            if (!userExists)
            {
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    CompanyId = company.Id,
                    Email = adminEmail,
                    FullName = string.IsNullOrWhiteSpace(request.AdminFullName) ? $"{company.Name} Admin" : request.AdminFullName.Trim(),
                    Phone = request.Phone?.Trim(),
                    PasswordHash = _passwordHasher.HashPassword(request.Password),
                    Role = "Admin",
                    Status = UserStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _context.Users.Add(adminUser);
            }
        }

        await _context.SaveChangesAsync();

        return CompanyMapper.ToDto(company);
    }

    public async Task<CompanyDto> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request)
    {
        var company = await _context.Companies.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == id);
        if (company == null)
        {
            throw new KeyNotFoundException("Không tìm thấy đối tác.");
        }

        company.Name = request.Name.Trim();
        company.TaxCode = request.TaxCode?.Trim();
        company.Phone = request.Phone?.Trim();
        company.Email = request.Email?.Trim();
        company.Website = request.Website?.Trim();
        company.Logo = request.Logo?.Trim();
        company.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return CompanyMapper.ToDto(company);
    }

    public async Task<bool> DeleteCompanyAsync(Guid id)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
        if (company != null)
        {
            company.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}
