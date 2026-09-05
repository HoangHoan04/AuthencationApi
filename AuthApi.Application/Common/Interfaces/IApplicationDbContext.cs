using AuthApi.Domain.Entities.Administrative;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Rbac;
using AuthApi.Domain.Entities.Security;
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
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<UserApp> UserApps { get; }
    DbSet<AuthClientSecret> AuthClientSecrets { get; }
    DbSet<AuthorizationCode> AuthorizationCodes { get; }
    DbSet<MfaDevice> MfaDevices { get; }
    DbSet<MfaBackupCode> MfaBackupCodes { get; }
    DbSet<EmailVerification> EmailVerifications { get; }
    DbSet<PasswordHistory> PasswordHistories { get; }
    DbSet<TokenDenylist> TokenDenylists { get; }
    DbSet<SigningKey> SigningKeys { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SystemSetting> SystemSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
