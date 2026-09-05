using System.Security.Cryptography;
using AuthApi.Application.Common.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Application.Common.Interfaces;

public interface IRsaKeyManager
{
    Task InitializeAsync();
    RSA GetSigningKey();
    string GetKeyId();
    JwksResponse GetJwks();
    IEnumerable<SecurityKey> GetValidationKeys();
    Task RotateAsync();
}
