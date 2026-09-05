using AuthApi.Application.Common.Interfaces;
using AuthApi.Infrastructure.BackgroundWorkers;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Security;
using AuthApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AuthApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b =>
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.ConfigureWarnings(w => w.Ignore(
                RelationalEventId.PendingModelChangesWarning,
                CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisConnection = configuration.GetConnectionString("Redis")
                ?? configuration["Redis:Connection"]
                ?? "localhost:6379";
            var options = ConfigurationOptions.Parse(redisConnection, true);
            options.AbortOnConnectFail = false;
            var mux = ConnectionMultiplexer.Connect(options);
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Redis");
            if (!mux.IsConnected)
            {
                var env = sp.GetRequiredService<IHostEnvironment>();
                if (env.IsProduction())
                {
                    throw new InvalidOperationException(
                        $"Cannot connect to Redis at '{redisConnection}'. Set ConnectionStrings:Redis.");
                }

                logger.LogWarning("Redis is not connected at {Connection}. Denylist/rate-limit will retry.", redisConnection);
            }

            return mux;
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IPasswordPolicy, PasswordPolicy>();
        services.AddSingleton<IDataProtectionService, DataProtectionService>();
        services.AddSingleton<IRsaKeyManager, RsaKeyManager>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOauthService, OauthService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserAccessService, UserAccessService>();
        services.AddScoped<ITokenDenylist, TokenDenylistService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddHostedService<TokenCleanupWorker>();

        return services;
    }
}
