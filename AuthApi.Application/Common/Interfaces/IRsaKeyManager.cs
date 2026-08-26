using System.Security.Cryptography;
using AuthApi.Application.Common.Models;

namespace AuthApi.Application.Common.Interfaces;

public interface IRsaKeyManager
{
    RSA GetSigningKey();
    string GetKeyId();
    JwksResponse GetJwks();
}
