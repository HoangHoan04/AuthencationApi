using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Common;
using AuthApi.Domain.Entities.Administrative;
using AuthApi.Domain.Entities.Auth;
using AuthApi.Domain.Entities.Companies;
using AuthApi.Domain.Entities.EcosystemApps;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Soft Delete Filter
        modelBuilder.Entity<Company>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EcosystemApp>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Province>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Ward>().HasQueryFilter(e => !e.IsDeleted);

        // EcosystemApp
        modelBuilder.Entity<EcosystemApp>(b =>
        {
            b.ToTable("ecosystem_apps");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(50).IsRequired();
            b.Property(e => e.Name).HasMaxLength(255).IsRequired();
            b.Property(e => e.Url).HasMaxLength(500).IsRequired();
            b.HasIndex(e => e.Code).IsUnique();
        });

        // Company
        modelBuilder.Entity<Company>(b =>
        {
            b.ToTable("companies");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(50).IsRequired();
            b.Property(e => e.Name).HasMaxLength(255).IsRequired();
            b.HasIndex(e => e.Code).IsUnique();
        });

        // Province (Tỉnh / Thành phố)
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

        // Ward (Phường / Xã)
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

        // User
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(e => e.Id);
            b.Property(e => e.Email).HasMaxLength(255).IsRequired();
            b.Property(e => e.Phone).HasMaxLength(50);
            b.Property(e => e.FullName).HasMaxLength(255).IsRequired();
            b.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            b.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            b.HasIndex(e => e.Email).IsUnique();

            b.HasOne(e => e.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // RefreshToken
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

            b.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LoginHistory
        modelBuilder.Entity<LoginHistory>(b =>
        {
            b.ToTable("login_history");
            b.HasKey(e => e.Id);
            b.Property(e => e.EmailAttempted).HasMaxLength(255).IsRequired();
            b.Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
            b.Property(e => e.IpAddress).HasMaxLength(45);
            b.Property(e => e.Location).HasMaxLength(255);
            b.Property(e => e.FailureReason).HasMaxLength(255);

            b.HasOne(e => e.User)
                .WithMany(u => u.LoginHistories)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PasswordReset
        modelBuilder.Entity<PasswordReset>(b =>
        {
            b.ToTable("password_resets");
            b.HasKey(e => e.Id);
            b.Property(e => e.TokenHash).HasMaxLength(500).IsRequired();

            b.HasOne(e => e.User)
                .WithMany(u => u.PasswordResets)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
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
