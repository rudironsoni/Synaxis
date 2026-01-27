namespace Synaxis.InferenceGateway.Application.Security;

public interface ITokenVault
{
    Task<byte[]> EncryptAsync(Guid tenantId, string plaintext, CancellationToken cancellationToken = default);
    Task<string> DecryptAsync(Guid tenantId, byte[] ciphertext, CancellationToken cancellationToken = default);
    Task RotateKeyAsync(Guid tenantId, string newKeyBase64, CancellationToken cancellationToken = default);
}