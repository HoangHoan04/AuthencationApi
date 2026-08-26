namespace AuthApi.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    Guid? CompanyId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
