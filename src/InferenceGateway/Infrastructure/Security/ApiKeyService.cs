using System.Security.Cryptography;
using System.Text;
using Synaxis.InferenceGateway.Application.Security;

namespace Synaxis.InferenceGateway.Infrastructure.Security;

public sealed class ApiKeyService : IApiKeyService
{
    private const string Prefix = "sk-synaxis-";

    public string GenerateKey()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        // Use base62 or hex. Hex is simpler.
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"{Prefix}{hex}";
    }

    public string HashKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool ValidateKey(string key, string hash)
    {
        var computed = HashKey(key);
        // Constant time comparison
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(hash));
    }
}