namespace Synaxis.InferenceGateway.Application.Security;

public interface IApiKeyService
{
    string GenerateKey();
    string HashKey(string key);
    bool ValidateKey(string key, string hash);
}