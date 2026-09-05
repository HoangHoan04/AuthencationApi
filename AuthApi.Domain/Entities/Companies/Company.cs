using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.Administrative;
using AuthApi.Domain.Entities.Users;

namespace AuthApi.Domain.Entities.Companies;

public class Company : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Logo { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? ParentCompanyId { get; set; }
    public string? ContactName { get; set; }
    public string? Address { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? WardId { get; set; }
    public string Country { get; set; } = "VN";
    public string? PlanTier { get; set; }
    public int? MaxUsers { get; set; }
    public string? SettingsJson { get; set; }

    public virtual Company? ParentCompany { get; set; }
    public virtual ICollection<Company> ChildCompanies { get; set; } = new List<Company>();
    public virtual Province? Province { get; set; }
    public virtual Ward? Ward { get; set; }
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
