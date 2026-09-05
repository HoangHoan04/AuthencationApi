using System.Security.Cryptography;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Entities.Security;
using AuthApi.Domain.Enums;
using AuthApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Infrastructure.Security;

public class RsaKeyManager : IRsaKeyManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RsaKeyManager> _logger;
    private readonly object _sync = new();
    private RSA? _signingRsa;
    private string _keyId = "auth-key-v1";
    private JwksResponse _jwks = new();
    private List<RsaSecurityKey> _validationKeys = new();

    public RsaKeyManager(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RsaKeyManager> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var protection = scope.ServiceProvider.GetRequiredService<IDataProtectionService>();

        var keys = await db.SigningKeys
            .Where(k => k.Status != SigningKeyStatus.Retired)
            .OrderByDescending(k => k.Status == SigningKeyStatus.Active)
            .ThenByDescending(k => k.CreatedAt)
            .ToListAsync();

        if (keys.Count == 0)
        {
            var rsa = RSA.Create(2048);
            var kid = _configuration["Jwt:KeyId"] ?? $"auth-key-{DateTime.UtcNow:yyyyMMdd}-v1";
            var entity = new SigningKey
            {
                Id = Guid.NewGuid(),
                KeyId = kid,
                Algorithm = "RS256",
                Use = "sig",
                PrivateKeyPemEncrypted = protection.Encrypt(rsa.ExportPkcs8PrivateKeyPem()),
                PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem(),
                Status = SigningKeyStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.SigningKeys.Add(entity);
            await db.SaveChangesAsync();
            keys.Add(entity);
            _logger.LogInformation("Created signing key {KeyId}", kid);
        }

        ReloadFromEntities(keys, protection);
    }

    public RSA GetSigningKey()
    {
        EnsureInitialized();
        return _signingRsa!;
    }

    public string GetKeyId()
    {
        EnsureInitialized();
        return _keyId;
    }

    public JwksResponse GetJwks()
    {
        EnsureInitialized();
        return _jwks;
    }

    public IEnumerable<SecurityKey> GetValidationKeys()
    {
        EnsureInitialized();
        return _validationKeys;
    }

    public async Task RotateAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var protection = scope.ServiceProvider.GetRequiredService<IDataProtectionService>();

        var active = await db.SigningKeys.Where(k => k.Status == SigningKeyStatus.Active).ToListAsync();
        foreach (var key in active)
        {
            key.Status = SigningKeyStatus.Rotating;
            key.RotatedAt = DateTimeOffset.UtcNow;
            key.RetireAfter = DateTimeOffset.UtcNow.AddHours(24);
        }

        var rsa = RSA.Create(2048);
        var kid = $"auth-key-{DateTime.UtcNow:yyyyMMddHHmmss}";
        db.SigningKeys.Add(new SigningKey
        {
            Id = Guid.NewGuid(),
            KeyId = kid,
            Algorithm = "RS256",
            Use = "sig",
            PrivateKeyPemEncrypted = protection.Encrypt(rsa.ExportPkcs8PrivateKeyPem()),
            PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem(),
            Status = SigningKeyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var keys = await db.SigningKeys.Where(k => k.Status != SigningKeyStatus.Retired).ToListAsync();
        ReloadFromEntities(keys, protection);
        _logger.LogInformation("Rotated signing key. Active kid={KeyId}", kid);
    }

    private void EnsureInitialized()
    {
        if (_signingRsa != null)
        {
            return;
        }

        lock (_sync)
        {
            if (_signingRsa != null)
            {
                return;
            }

            InitializeAsync().GetAwaiter().GetResult();
        }
    }

    private void ReloadFromEntities(List<SigningKey> keys, IDataProtectionService protection)
    {
        lock (_sync)
        {
            var active = keys.FirstOrDefault(k => k.Status == SigningKeyStatus.Active) ?? keys[0];
            var rsa = RSA.Create();
            rsa.ImportFromPem(protection.Decrypt(active.PrivateKeyPemEncrypted));
            _signingRsa?.Dispose();
            _signingRsa = rsa;
            _keyId = active.KeyId;

            _validationKeys = new List<RsaSecurityKey>();
            var jwks = new List<JwkKeyDto>();
            foreach (var key in keys)
            {
                var pub = RSA.Create();
                pub.ImportFromPem(key.PublicKeyPem);
                var parameters = pub.ExportParameters(false);
                var securityKey = new RsaSecurityKey(pub) { KeyId = key.KeyId };
                _validationKeys.Add(securityKey);
                jwks.Add(new JwkKeyDto
                {
                    Kid = key.KeyId,
                    N = Base64UrlEncoder.Encode(parameters.Modulus!),
                    E = Base64UrlEncoder.Encode(parameters.Exponent!)
                });
            }

            _jwks = new JwksResponse { Keys = jwks };
        }
    }
}
