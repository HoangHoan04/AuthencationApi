using System.Security.Cryptography;
using System.Text;
using AuthApi.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AuthApi.Infrastructure.Security;

public class DataProtectionService : IDataProtectionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public DataProtectionService(IConfiguration configuration, IHostEnvironment environment)
    {
        var secret = configuration["Security:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException("Security:EncryptionKey is required in production.");
            }

            secret = "DEV-ONLY-AUTH-ENCRYPTION-KEY-CHANGE-ME";
        }

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return ciphertext;
        }

        var buffer = Convert.FromBase64String(ciphertext);
        if (buffer.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Ciphertext is too short.");
        }

        var nonce = buffer.AsSpan(0, NonceSize);
        var tag = buffer.AsSpan(NonceSize, TagSize);
        var encrypted = buffer.AsSpan(NonceSize + TagSize);
        var plaintextBytes = new byte[encrypted.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, encrypted, tag, plaintextBytes);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
