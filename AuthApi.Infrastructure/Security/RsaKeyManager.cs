using System.Security.Cryptography;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Infrastructure.Security;

public class RsaKeyManager : IRsaKeyManager
{
    private readonly RSA _rsa;
    private readonly string _keyId;
    private readonly JwksResponse _cachedJwks;

    public RsaKeyManager(IConfiguration configuration, ILogger<RsaKeyManager> logger)
    {
        _rsa = RSA.Create(2048);

        // Check if existing PEM key is provided in config or key file
        var privateKeyPem = configuration["Jwt:PrivateKeyPem"];
        var keyPath = Path.Combine(AppContext.BaseDirectory, "rsa_private_key.pem");

        if (!string.IsNullOrWhiteSpace(privateKeyPem))
        {
            _rsa.ImportFromPem(privateKeyPem);
            logger.LogInformation("Loaded RSA Signing Key from Configuration.");
        }
        else if (File.Exists(keyPath))
        {
            var pem = File.ReadAllText(keyPath);
            _rsa.ImportFromPem(pem);
            logger.LogInformation("Loaded RSA Signing Key from file: {Path}", keyPath);
        }
        else
        {
            // Auto generate and persist key for consistency across app restarts
            var pem = _rsa.ExportPkcs8PrivateKeyPem();
            try
            {
                File.WriteAllText(keyPath, pem);
                logger.LogInformation("Generated and saved new RSA 2048-bit Key to {Path}", keyPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not persist RSA Key to disk, using in-memory key.");
            }
        }

        var rsaParams = _rsa.ExportParameters(false);
        var modulusBase64Url = Base64UrlEncoder.Encode(rsaParams.Modulus!);
        var exponentBase64Url = Base64UrlEncoder.Encode(rsaParams.Exponent!);

        _keyId = configuration["Jwt:KeyId"] ?? "auth-key-v1";

        _cachedJwks = new JwksResponse
        {
            Keys = new List<JwkKeyDto>
            {
                new()
                {
                    Kty = "RSA",
                    Use = "sig",
                    Kid = _keyId,
                    Alg = "RS256",
                    N = modulusBase64Url,
                    E = exponentBase64Url
                }
            }
        };
    }

    public RSA GetSigningKey() => _rsa;

    public string GetKeyId() => _keyId;

    public JwksResponse GetJwks() => _cachedJwks;
}
