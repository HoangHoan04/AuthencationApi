using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AuthApi.Infrastructure.Persistence;

public static class DatabaseBootstrap
{
    public const int Utf8EncodingId = 6;

    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        try
        {
            await EnsureUtf8DatabaseAsync(connectionString, logger);

            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var passwordHasher = services.GetRequiredService<IPasswordHasher>();
            await SeedDefaultAdminUserAsync(context, passwordHasher, logger);

            logger.LogInformation("Auth Database initialized successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or initializing the Auth database.");
            throw;
        }
    }

    private static async Task EnsureUtf8DatabaseAsync(string connectionString, ILogger logger)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var targetDatabase = builder.Database;

        if (string.IsNullOrWhiteSpace(targetDatabase))
        {
            return;
        }

        builder.Database = "postgres";
        var masterConnStr = builder.ConnectionString;

        await using var conn = new NpgsqlConnection(masterConnStr);
        await conn.OpenAsync();

        var encodingId = await GetDatabaseEncodingIdAsync(conn, targetDatabase);
        if (encodingId == null)
        {
            logger.LogInformation("Database '{TargetDatabase}' does not exist. Creating with UTF8...", targetDatabase);
            await CreateUtf8DatabaseAsync(conn, targetDatabase);
        }
        else if (encodingId != Utf8EncodingId)
        {
            logger.LogWarning("Database '{TargetDatabase}' has non-UTF8 encoding (id={EncodingId}). Recreating with UTF8...", targetDatabase, encodingId);
            await using var dropCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{targetDatabase}\" WITH (FORCE);", conn);
            await dropCmd.ExecuteNonQueryAsync();

            await CreateUtf8DatabaseAsync(conn, targetDatabase);
        }
    }

    private static async Task CreateUtf8DatabaseAsync(NpgsqlConnection conn, string databaseName)
    {
        var createSql = $"""
            CREATE DATABASE "{databaseName}"
            WITH ENCODING 'UTF8'
                 LC_COLLATE='C'
                 LC_CTYPE='C'
                 TEMPLATE template0;
        """;
        await using var createCmd = new NpgsqlCommand(createSql, conn);
        await createCmd.ExecuteNonQueryAsync();
    }

    private static async Task<int?> GetDatabaseEncodingIdAsync(NpgsqlConnection conn, string databaseName)
    {
        var sql = "SELECT encoding FROM pg_database WHERE datname = @dbname;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("dbname", databaseName);
        var res = await cmd.ExecuteScalarAsync();
        return res is null or DBNull ? null : Convert.ToInt32(res);
    }

    private static async Task SeedDefaultAdminUserAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger)
    {
        var adminEmail = "admin@company.com";
        var adminUser = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                CompanyId = null,
                Email = adminEmail,
                Phone = "0901234567",
                FullName = "Hệ thống Quản trị viên (Super Admin)",
                Role = "SuperAdmin",
                PasswordHash = passwordHasher.HashPassword("Admin@123456"),
                Status = UserStatus.Active,
                FailedLoginAttempts = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default superadmin user: {Email} / Admin@123456", adminEmail);
        }
        else if (adminUser.Role != "SuperAdmin")
        {
            adminUser.Role = "SuperAdmin";
            await context.SaveChangesAsync();
        }
    }
}
