using System.Text.Json.Serialization;

namespace AuthApi.Application.Common.Models;

public class JwksResponse
{
    [JsonPropertyName("keys")]
    public List<JwkKeyDto> Keys { get; set; } = new();
}

public class JwkKeyDto
{
    [JsonPropertyName("kty")]
    public string Kty { get; set; } = "RSA";

    [JsonPropertyName("use")]
    public string Use { get; set; } = "sig";

    [JsonPropertyName("kid")]
    public string Kid { get; set; } = string.Empty;

    [JsonPropertyName("alg")]
    public string Alg { get; set; } = "RS256";

    [JsonPropertyName("n")]
    public string N { get; set; } = string.Empty;

    [JsonPropertyName("e")]
    public string E { get; set; } = string.Empty;
}
