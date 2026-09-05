using AuthApi.Application.Common;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.DTOs.Users;
using AuthApi.Domain.Entities.Users;

namespace AuthApi.Application.Common.Models;

public sealed class AccessTokenClaims
{
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Apps { get; init; } = Array.Empty<string>();
    public string? Jti { get; init; }
    public int? ExpiresMinutes { get; init; }
}

public static class UserProfileFactory
{
    public static UserProfileDto From(User user, UserAccessSnapshot? access = null)
    {
        var roles = access?.Roles.Count > 0
            ? access.Roles
            : user.UserRoles
                .Select(ur => ur.Role?.Code)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Cast<string>()
                .Distinct()
                .ToList();

        return new UserProfileDto
        {
            Id = user.Id,
            CompanyId = user.CompanyId,
            CompanyCode = user.Company?.Code,
            CompanyName = user.Company?.Name,
            Email = user.Email,
            Phone = user.Phone,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? RoleCodes.User,
            Roles = roles.ToList(),
            Permissions = access?.Permissions.ToList() ?? new List<string>(),
            Apps = access?.Apps.ToList() ?? new List<string>(),
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            MustChangePassword = user.MustChangePassword,
            MfaEnabled = user.MfaEnabled
        };
    }
}
