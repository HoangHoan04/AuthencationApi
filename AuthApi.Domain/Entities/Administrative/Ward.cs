using AuthApi.Domain.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Administrative;

public class Ward : BaseEntity
{
    public Guid ProvinceId { get; set; }
    public string ProvinceCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public WardDivisionType DivisionType { get; set; } = WardDivisionType.Commune;
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public virtual Province? Province { get; set; }
}
