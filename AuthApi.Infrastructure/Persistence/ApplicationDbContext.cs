using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.Administrative;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Entities.EcosystemApps;
using AuthApi.Domain.Entities.Rbac;
using AuthApi.Domain.Entities.Security;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EcosystemApp> EcosystemApps => Set<EcosystemApp>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserApp> UserApps => Set<UserApp>();
    public DbSet<AuthClientSecret> AuthClientSecrets => Set<AuthClientSecret>();
    public DbSet<AuthorizationCode> AuthorizationCodes => Set<AuthorizationCode>();
    public DbSet<MfaDevice> MfaDevices => Set<MfaDevice>();
    public DbSet<MfaBackupCode> MfaBackupCodes => Set<MfaBackupCode>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<TokenDenylist> TokenDenylists => Set<TokenDenylist>();
    public DbSet<SigningKey> SigningKeys => Set<SigningKey>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureBaseEntities(modelBuilder);

        modelBuilder.Entity<Company>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EcosystemApp>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Province>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Ward>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Permission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UserRole>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UserApp>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AuthClientSecret>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MfaDevice>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SigningKey>().HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<SystemSetting>(b =>
        {
            b.ToTable("system_settings");
            b.HasKey(e => e.Id);
            b.Property(e => e.Key).HasMaxLength(150).IsRequired();
            b.HasIndex(e => e.Key).IsUnique();
            b.Property(e => e.Value).IsRequired();
            b.Property(e => e.Group).HasMaxLength(50).IsRequired();
            b.Property(e => e.ValueType).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<EcosystemApp>(b =>
        {
            b.ToTable("ecosystem_apps");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(50).IsRequired();
            b.Property(e => e.Name).HasMaxLength(255).IsRequired();
            b.Property(e => e.Url).HasMaxLength(500).IsRequired();
            b.Property(e => e.ClientId).HasMaxLength(100);
            b.Property(e => e.ClientSecretHash).HasMaxLength(500);
            b.Property(e => e.AppType).HasConversion<string>().HasMaxLength(50);
            b.Property(e => e.GrantTypesJson).HasColumnType("jsonb");
            b.Property(e => e.ScopesJson).HasColumnType("jsonb");
            b.Property(e => e.RedirectUrlsJson).HasColumnType("jsonb");
            b.Property(e => e.AllowedOriginsJson).HasColumnType("jsonb");
            b.HasIndex(e => e.Code).IsUnique();
            b.HasIndex(e => e.ClientId).IsUnique().HasFilter("\"ClientId\" IS NOT NULL AND \"ClientId\" <> ''");
        });

        modelBuilder.Entity<Company>(b =>
        {
            b.ToTable("companies");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(50).IsRequired();
            b.Property(e => e.Name).HasMaxLength(255).IsRequired();
            b.Property(e => e.Country).HasMaxLength(8);
            b.Property(e => e.PlanTier).HasMaxLength(50);
            b.Property(e => e.SettingsJson).HasColumnType("jsonb");
            b.HasIndex(e => e.Code).IsUnique();
            b.HasOne(e => e.ParentCompany)
                .WithMany(c => c.ChildCompanies)
                .HasForeignKey(e => e.ParentCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Province)
                .WithMany()
                .HasForeignKey(e => e.ProvinceId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(e => e.Ward)
                .WithMany()
                .HasForeignKey(e => e.WardId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Province>(b =>
        {
            b.ToTable("provinces");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(20).IsRequired();
            b.Property(e => e.Name).HasMaxLength(255).IsRequired();
            b.Property(e => e.FullName).HasMaxLength(255).IsRequired();
            b.Property(e => e.DivisionType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(ProvinceDivisionType.Province)
                .HasSentinel((ProvinceDivisionType)0);
            b.Property(e => e.AdministrativeRegion).HasMaxLength(100);
            b.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<Ward>(b =>
        {
            b.ToTable("wards");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(20).IsRequired();
            b.Property(e => e.Name).HasMaxLength(255).IsRequired();
            b.Property(e => e.FullName).HasMaxLength(255).IsRequired();
            b.Property(e => e.ProvinceCode).HasMaxLength(20).IsRequired();
            b.Property(e => e.DivisionType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(WardDivisionType.Commune)
                .HasSentinel((WardDivisionType)0);
            b.HasIndex(e => e.Code).IsUnique();
            b.HasIndex(e => e.ProvinceId);
            b.HasIndex(e => e.ProvinceCode);
            b.HasOne(e => e.Province)
                .WithMany(p => p.Wards)
                .HasForeignKey(e => e.ProvinceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(e => e.Id);
            b.Property(e => e.Email).HasMaxLength(255).IsRequired();
            b.Property(e => e.Phone).HasMaxLength(50);
            b.Property(e => e.FullName).HasMaxLength(255).IsRequired();
            b.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            b.Property(e => e.Locale).HasMaxLength(20);
            b.Property(e => e.Timezone).HasMaxLength(100);
            b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            b.Property(e => e.UserType).HasConversion<string>().HasMaxLength(50);
            b.HasIndex(e => e.Email).IsUnique();
            b.HasIndex(e => e.CompanyId);
            b.HasOne(e => e.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(e => e.Id);
            b.Property(e => e.TokenHash).HasMaxLength(500).IsRequired();
            b.Property(e => e.DeviceName).HasMaxLength(255);
            b.Property(e => e.IpAddress).HasMaxLength(45);
            b.HasIndex(e => e.TokenHash);
            b.HasIndex(e => e.UserId);
            b.HasIndex(e => e.FamilyId);
            b.HasIndex(e => e.AppId);
            b.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.App)
                .WithMany(a => a.RefreshTokens)
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LoginHistory>(b =>
        {
            b.ToTable("login_history");
            b.HasKey(e => e.Id);
            b.Property(e => e.EmailAttempted).HasMaxLength(255).IsRequired();
            b.Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
            b.Property(e => e.IpAddress).HasMaxLength(45);
            b.Property(e => e.Location).HasMaxLength(255);
            b.Property(e => e.GeoCountry).HasMaxLength(8);
            b.Property(e => e.CorrelationId).HasMaxLength(64);
            b.Property(e => e.FailureReason).HasMaxLength(255);
            b.HasIndex(e => e.CreatedAt);
            b.HasOne(e => e.User)
                .WithMany(u => u.LoginHistories)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(e => e.App)
                .WithMany()
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PasswordReset>(b =>
        {
            b.ToTable("password_resets");
            b.HasKey(e => e.Id);
            b.Property(e => e.TokenHash).HasMaxLength(500).IsRequired();
            b.Property(e => e.OtpHash).HasMaxLength(500);
            b.Property(e => e.RequestIp).HasMaxLength(45);
            b.HasOne(e => e.User)
                .WithMany(u => u.PasswordResets)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("roles");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(80).IsRequired();
            b.Property(e => e.Name).HasMaxLength(150).IsRequired();
            b.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
            b.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Permission>(b =>
        {
            b.ToTable("permissions");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(120).IsRequired();
            b.Property(e => e.Name).HasMaxLength(200).IsRequired();
            b.Property(e => e.Resource).HasMaxLength(80).IsRequired();
            b.Property(e => e.Action).HasMaxLength(40).IsRequired();
            b.Property(e => e.Module).HasMaxLength(50);
            b.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("role_permissions");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            b.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.ToTable("user_roles");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.UserId, e.RoleId, e.AppId, e.CompanyId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            b.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.App)
                .WithMany(a => a.UserRoles)
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserApp>(b =>
        {
            b.ToTable("user_apps");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.UserId, e.AppId }).IsUnique();
            b.HasOne(e => e.User)
                .WithMany(u => u.UserApps)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.App)
                .WithMany(a => a.UserApps)
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuthClientSecret>(b =>
        {
            b.ToTable("auth_client_secrets");
            b.HasKey(e => e.Id);
            b.Property(e => e.SecretHash).HasMaxLength(500).IsRequired();
            b.Property(e => e.SecretPrefix).HasMaxLength(32);
            b.HasIndex(e => e.AppId);
            b.HasOne(e => e.App)
                .WithMany(a => a.ClientSecrets)
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuthorizationCode>(b =>
        {
            b.ToTable("authorization_codes");
            b.HasKey(e => e.Id);
            b.Property(e => e.CodeHash).HasMaxLength(500).IsRequired();
            b.Property(e => e.RedirectUri).HasMaxLength(1000).IsRequired();
            b.Property(e => e.CodeChallenge).HasMaxLength(255);
            b.Property(e => e.CodeChallengeMethod).HasMaxLength(20);
            b.HasIndex(e => e.CodeHash);
            b.HasIndex(e => e.ExpiresAt);
            b.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.App)
                .WithMany()
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MfaDevice>(b =>
        {
            b.ToTable("mfa_devices");
            b.HasKey(e => e.Id);
            b.Property(e => e.Method).HasConversion<string>().HasMaxLength(30);
            b.Property(e => e.Name).HasMaxLength(100);
            b.HasOne(e => e.User)
                .WithMany(u => u.MfaDevices)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MfaBackupCode>(b =>
        {
            b.ToTable("mfa_backup_codes");
            b.HasKey(e => e.Id);
            b.Property(e => e.CodeHash).HasMaxLength(500).IsRequired();
            b.HasOne(e => e.User)
                .WithMany(u => u.MfaBackupCodes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailVerification>(b =>
        {
            b.ToTable("email_verifications");
            b.HasKey(e => e.Id);
            b.Property(e => e.TokenHash).HasMaxLength(500).IsRequired();
            b.Property(e => e.Email).HasMaxLength(255).IsRequired();
            b.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordHistory>(b =>
        {
            b.ToTable("password_histories");
            b.HasKey(e => e.Id);
            b.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            b.HasOne(e => e.User)
                .WithMany(u => u.PasswordHistories)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TokenDenylist>(b =>
        {
            b.ToTable("token_denylist");
            b.HasKey(e => e.Id);
            b.Property(e => e.Jti).HasMaxLength(64).IsRequired();
            b.Property(e => e.Reason).HasMaxLength(255);
            b.HasIndex(e => e.Jti).IsUnique();
            b.HasIndex(e => e.ExpiresAt);
        });

        modelBuilder.Entity<SigningKey>(b =>
        {
            b.ToTable("signing_keys");
            b.HasKey(e => e.Id);
            b.Property(e => e.KeyId).HasMaxLength(80).IsRequired();
            b.Property(e => e.Algorithm).HasMaxLength(20);
            b.Property(e => e.Use).HasMaxLength(10);
            b.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            b.HasIndex(e => e.KeyId).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs");
            b.HasKey(e => e.Id);
            b.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
            b.Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
            b.Property(e => e.Summary).HasMaxLength(500);
            b.Property(e => e.IpAddress).HasMaxLength(45);
            b.Property(e => e.CorrelationId).HasMaxLength(64);
            b.Property(e => e.BeforeJson).HasColumnType("jsonb");
            b.Property(e => e.AfterJson).HasColumnType("jsonb");
            b.HasIndex(e => e.CreatedAt);
            b.HasIndex(e => new { e.EntityType, e.EntityId });
        });
    }

    private static void ConfigureBaseEntities(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IHasConcurrency).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(IHasConcurrency.RowVersion))
                .IsRowVersion();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var currentUserId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                if (entry.Entity.CreatedBy == null && currentUserId.HasValue)
                {
                    entry.Entity.CreatedBy = currentUserId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                if (currentUserId.HasValue)
                {
                    entry.Entity.UpdatedBy = currentUserId;
                }
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
