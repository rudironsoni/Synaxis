namespace Synaxis.Common.Tests;

/// <summary>
/// Interface for embedding generation.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IEmbeddingProvider
{
    Task<float[]> GenerateEmbeddingAsync(string text, string model, CancellationToken cancellationToken);
}
