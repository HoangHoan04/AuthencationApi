using AuthApi.Domain.Entities.Administrative;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<User> Users { get; }
    DbSet<EcosystemApp> EcosystemApps { get; }
    DbSet<Province> Provinces { get; }
    DbSet<Ward> Wards { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<LoginHistory> LoginHistories { get; }
    DbSet<PasswordReset> PasswordResets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
