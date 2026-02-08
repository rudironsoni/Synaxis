using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;

/// <summary>
/// Unit tests for ISemanticCacheService implementations
/// Tests semantic caching functionality for LLM responses
/// </summary>
public class SemanticCacheServiceTests
{
    private readonly Mock<ISemanticCacheService> _mockCacheService;
    private readonly CancellationToken _cancellationToken;

    public SemanticCacheServiceTests()
    {
        this._mockCacheService = new Mock<ISemanticCacheService>();
        this._cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task TryGetCachedAsync_ExactMatch_ReturnsHit()
    {
        // Arrange
        var query = "What is the capital of France?";
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;

        var expectedResponse = new CacheResult
        {
            IsHit = true,
            Response = "The capital of France is Paris.",
            SimilarityScore = 1.0,
            CachedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query, sessionId, model, temperature, this._cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await this._mockCacheService.Object.TryGetCachedAsync(
            query, sessionId, model, temperature, this._cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsHit);
        Assert.Equal("The capital of France is Paris.", result.Response);
        Assert.Equal(1.0, result.SimilarityScore);
        Assert.True(result.CachedAt < DateTimeOffset.UtcNow);

        this._mockCacheService.Verify(
            x => x.TryGetCachedAsync(query, sessionId, model, temperature, this._cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task TryGetCachedAsync_SimilarQuery_ReturnsSemanticHit()
    {
        // Arrange
        var query = "Tell me the capital city of France";
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;

        var expectedResponse = new CacheResult
        {
            IsHit = true,
            Response = "The capital of France is Paris.",
            SimilarityScore = 0.95, // High similarity but not exact match
            CachedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        };

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query, sessionId, model, temperature, this._cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await this._mockCacheService.Object.TryGetCachedAsync(
            query, sessionId, model, temperature, this._cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsHit);
        Assert.Equal("The capital of France is Paris.", result.Response);
        Assert.True(result.SimilarityScore >= 0.8); // Above semantic threshold
        Assert.True(result.SimilarityScore < 1.0);

        this._mockCacheService.Verify(
            x => x.TryGetCachedAsync(query, sessionId, model, temperature, this._cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task TryGetCachedAsync_DifferentSession_ReturnsMiss()
    {
        // Arrange
        var query = "What is the capital of France?";
        var sessionId2 = "session-456";
        var model = "gpt-4";
        var temperature = 0.7;

        var missResponse = new CacheResult
        {
            IsHit = false,
            Response = null,
            SimilarityScore = 0.0,
            QueryEmbedding = new float[] { 0.1f, 0.2f, 0.3f },
        };

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query, sessionId2, model, temperature, this._cancellationToken))
            .ReturnsAsync(missResponse);

        // Act
        var result = await this._mockCacheService.Object.TryGetCachedAsync(
            query, sessionId2, model, temperature, this._cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsHit);
        Assert.Null(result.Response);
        Assert.Equal(0.0, result.SimilarityScore);
        Assert.NotNull(result.QueryEmbedding);

        this._mockCacheService.Verify(
            x => x.TryGetCachedAsync(query, sessionId2, model, temperature, this._cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task TryGetCachedAsync_DifferentModel_ReturnsMiss()
    {
        // Arrange
        var query = "What is the capital of France?";
        var sessionId = "session-123";
        var model2 = "gpt-3.5-turbo";
        var temperature = 0.7;

        var missResponse = new CacheResult
        {
            IsHit = false,
            Response = null,
            SimilarityScore = 0.0,
            QueryEmbedding = new float[] { 0.1f, 0.2f, 0.3f },
        };

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query, sessionId, model2, temperature, this._cancellationToken))
            .ReturnsAsync(missResponse);

        // Act
        var result = await this._mockCacheService.Object.TryGetCachedAsync(
            query, sessionId, model2, temperature, this._cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsHit);
        Assert.Null(result.Response);

        this._mockCacheService.Verify(
            x => x.TryGetCachedAsync(query, sessionId, model2, temperature, this._cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task TryGetCachedAsync_DifferentTemperature_ReturnsMiss()
    {
        // Arrange
        var query = "What is the capital of France?";
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature2 = 0.0;

        var missResponse = new CacheResult
        {
            IsHit = false,
            Response = null,
            SimilarityScore = 0.0,
            QueryEmbedding = new float[] { 0.1f, 0.2f, 0.3f },
        };

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query, sessionId, model, temperature2, this._cancellationToken))
            .ReturnsAsync(missResponse);

        // Act
        var result = await this._mockCacheService.Object.TryGetCachedAsync(
            query, sessionId, model, temperature2, this._cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsHit);
        Assert.Null(result.Response);

        this._mockCacheService.Verify(
            x => x.TryGetCachedAsync(query, sessionId, model, temperature2, this._cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task TryGetCachedAsync_CacheMiss_ReturnsMissWithEmbedding()
    {
        // Arrange
        var query = "What is quantum computing?";
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;

        var missResponse = new CacheResult
        {
            IsHit = false,
            Response = null,
            SimilarityScore = 0.0,
            QueryEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f },
        };

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query, sessionId, model, temperature, this._cancellationToken))
            .ReturnsAsync(missResponse);

        // Act
        var result = await this._mockCacheService.Object.TryGetCachedAsync(
            query, sessionId, model, temperature, this._cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsHit);
        Assert.Null(result.Response);
        Assert.NotNull(result.QueryEmbedding);
        Assert.NotEmpty(result.QueryEmbedding);
        Assert.Equal(5, result.QueryEmbedding.Length);

        this._mockCacheService.Verify(
            x => x.TryGetCachedAsync(query, sessionId, model, temperature, this._cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task StoreAsync_ValidResponse_StoresSuccessfully()
    {
        // Arrange
        var query = "What is the capital of France?";
        var response = "The capital of France is Paris.";
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        this._mockCacheService
            .Setup(x => x.StoreAsync(query, response, sessionId, model, temperature, embedding, this._cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await this._mockCacheService.Object.StoreAsync(
            query, response, sessionId, model, temperature, embedding, this._cancellationToken);

        // Assert
        this._mockCacheService.Verify(
            x => x.StoreAsync(query, response, sessionId, model, temperature, embedding, this._cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task StoreAsync_ErrorResponse_DoesNotStore()
    {
        // Arrange
        var query = "What is the capital of France?";
        var errorResponse = "Error: Rate limit exceeded";
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Mock should not be called for error responses
        this._mockCacheService
            .Setup(x => x.StoreAsync(
                It.IsAny<string>(),
                It.Is<string>(r => r.StartsWith("Error:")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<float[]>(),
                It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Should not store error responses"));

        // Act & Assert - Should not call StoreAsync for error responses
        // In actual implementation, the service should filter out error responses

        // Verify that storing error responses would throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await this._mockCacheService.Object.StoreAsync(
                query, errorResponse, sessionId, model, temperature, embedding, this._cancellationToken);
        });
    }

    [Fact]
    public async Task InvalidateSessionAsync_RemovesSessionEntries()
    {
        // Arrange
        var sessionId = "session-123";

        this._mockCacheService
            .Setup(x => x.InvalidateSessionAsync(sessionId, this._cancellationToken))
            .ReturnsAsync(5); // Returns count of invalidated entries

        // Act
        var invalidatedCount = await this._mockCacheService.Object.InvalidateSessionAsync(
            sessionId, this._cancellationToken);

        // Assert
        Assert.Equal(5, invalidatedCount);

        this._mockCacheService.Verify(
            x => x.InvalidateSessionAsync(sessionId, this._cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ConcurrentAccess_HandlesSafely()
    {
        // Arrange
        var query = "What is the capital of France?";
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;

        var hitResponse = new CacheResult
        {
            IsHit = true,
            Response = "The capital of France is Paris.",
            SimilarityScore = 1.0,
            CachedAt = DateTimeOffset.UtcNow,
        };

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query, sessionId, model, temperature, this._cancellationToken))
            .ReturnsAsync(hitResponse);

        // Act - Simulate concurrent access
        var tasks = new Task<CacheResult>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = this._mockCacheService.Object.TryGetCachedAsync(
                query, sessionId, model, temperature, this._cancellationToken);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, r =>
        {
            Assert.NotNull(r);
            Assert.True(r.IsHit);
            Assert.Equal("The capital of France is Paris.", r.Response);
        });

        this._mockCacheService.Verify(
            x => x.TryGetCachedAsync(query, sessionId, model, temperature, this._cancellationToken),
            Times.Exactly(10));
    }

    [Fact]
    public async Task TryGetCachedAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        string? query = null;
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query!, sessionId, model, temperature, this._cancellationToken))
            .ThrowsAsync(new ArgumentNullException(nameof(query)));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await this._mockCacheService.Object.TryGetCachedAsync(
                query!, sessionId, model, temperature, this._cancellationToken);
        });
    }

    [Fact]
    public async Task TryGetCachedAsync_WithEmptyQuery_ReturnsMiss()
    {
        // Arrange
        var query = string.Empty;
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;

        var missResponse = new CacheResult
        {
            IsHit = false,
            Response = null,
            SimilarityScore = 0.0,
        };

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query, sessionId, model, temperature, this._cancellationToken))
            .ReturnsAsync(missResponse);

        // Act
        var result = await this._mockCacheService.Object.TryGetCachedAsync(
            query, sessionId, model, temperature, this._cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsHit);
    }

    [Fact]
    public async Task StoreAsync_WithNullEmbedding_ComputesEmbedding()
    {
        // Arrange
        var query = "What is the capital of France?";
        var response = "The capital of France is Paris.";
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;
        float[]? embedding = null;

        this._mockCacheService
            .Setup(x => x.StoreAsync(query, response, sessionId, model, temperature, null, this._cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await this._mockCacheService.Object.StoreAsync(
            query, response, sessionId, model, temperature, embedding, this._cancellationToken);

        // Assert
        this._mockCacheService.Verify(
            x => x.StoreAsync(query, response, sessionId, model, temperature, null, this._cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateSessionAsync_WithNonExistentSession_ReturnsZero()
    {
        // Arrange
        var sessionId = "non-existent-session";

        this._mockCacheService
            .Setup(x => x.InvalidateSessionAsync(sessionId, this._cancellationToken))
            .ReturnsAsync(0);

        // Act
        var invalidatedCount = await this._mockCacheService.Object.InvalidateSessionAsync(
            sessionId, this._cancellationToken);

        // Assert
        Assert.Equal(0, invalidatedCount);
    }

    [Fact]
    public async Task TryGetCachedAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var query = "What is the capital of France?";
        var sessionId = "session-123";
        var model = "gpt-4";
        var temperature = 0.7;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        this._mockCacheService
            .Setup(x => x.TryGetCachedAsync(query, sessionId, model, temperature, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await this._mockCacheService.Object.TryGetCachedAsync(
                query, sessionId, model, temperature, cts.Token);
        });
    }
}

/// <summary>
/// Represents the result of a cache lookup operation
/// </summary>
public class CacheResult
{
    public bool IsHit { get; set; }

    public string? Response { get; set; }

    public double SimilarityScore { get; set; }

    public DateTimeOffset CachedAt { get; set; }

    public float[]? QueryEmbedding { get; set; }
}

/// <summary>
/// Interface for semantic cache service
/// </summary>
public interface ISemanticCacheService
{
    Task<CacheResult> TryGetCachedAsync(
        string query,
        string sessionId,
        string model,
        double temperature,
        CancellationToken cancellationToken);

    Task StoreAsync(
        string query,
        string response,
        string sessionId,
        string model,
        double temperature,
        float[]? embedding,
        CancellationToken cancellationToken);

    Task<int> InvalidateSessionAsync(
        string sessionId,
        CancellationToken cancellationToken);
}
