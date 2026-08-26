using AuthApi.Domain.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Administrative;

public class Province : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public ProvinceDivisionType DivisionType { get; set; } = ProvinceDivisionType.Province;
    public string? AdministrativeRegion { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
