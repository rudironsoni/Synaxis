using Moq;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;

namespace Synaxis.Benchmarks;

/// <summary>
/// Base class for all Synaxis tests providing common mocking infrastructure and setup.
/// Provides factory methods for creating mock objects with sensible defaults.
/// </summary>
public abstract class TestBase
{
    /// <summary>
    /// Creates a mock ILogger for any type.
    /// </summary>
    protected Mock<ILogger<T>> CreateMockLogger<T>() where T : class
    {
        var mock = new Mock<ILogger<T>>();
        mock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!))
            .Verifiable();
        return mock;
    }

    /// <summary>
    /// Creates a mock IChatClient that returns a default response.
    /// </summary>
    protected Mock<IChatClient> CreateMockChatClient(string responseText = "Mock response")
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(x => x.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, responseText)));
        return mock;
    }

    /// <summary>
    /// Creates a mock IChatClient that returns a streaming response.
    /// </summary>
    protected Mock<IChatClient> CreateMockStreamingChatClient(params string[] responseChunks)
    {
        var mock = new Mock<IChatClient>();
        
        mock.Setup(x => x.GetStreamingResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()))
            .Returns(GenerateStreamingResponse(responseChunks));
        
        return mock;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> GenerateStreamingResponse(string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            await Task.Yield();
        }
    }

    /// <summary>
    /// Creates a mock IProviderRegistry with the specified provider configurations.
    /// </summary>
    protected Mock<IProviderRegistry> CreateMockProviderRegistry(Dictionary<string, ProviderConfig>? providers = null)
    {
        var mock = new Mock<IProviderRegistry>();
        
        providers ??= new Dictionary<string, ProviderConfig>
        {
            ["groq"] = new ProviderConfig { Type = "groq", Tier = 0, Models = ["llama-3.1-70b-versatile"] },
            ["openai"] = new ProviderConfig { Type = "openai", Tier = 1, Models = ["gpt-4"] }
        };

        mock.Setup(x => x.GetProvider(It.IsAny<string>()))
            .Returns((string key) => providers.TryGetValue(key, out var provider) ? provider : null);

        mock.Setup(x => x.GetCandidates(It.IsAny<string>()))
            .Returns((string modelId) => providers
                .Where(p => p.Value.Models.Contains(modelId) || p.Value.Models.Contains("*"))
                .Select(p => (p.Key, p.Value.Tier)));

        return mock;
    }

    /// <summary>
    /// Creates a mock IModelResolver that returns a default resolution.
    /// </summary>
    protected Mock<IModelResolver> CreateMockModelResolver(string modelId = "test-model", string canonicalId = "test-canonical")
    {
        var mock = new Mock<IModelResolver>();
        mock.Setup(x => x.Resolve(
            It.IsAny<string>(),
            It.IsAny<RequiredCapabilities?>()))
            .Returns(new ResolutionResult(
                modelId,
                new CanonicalModelId(canonicalId, canonicalId),
                new List<ProviderConfig> { new() { Key = "test-provider" } }));
        mock.Setup(x => x.ResolveAsync(
            It.IsAny<string>(),
            It.IsAny<EndpointKind>(),
            It.IsAny<RequiredCapabilities?>(),
            It.IsAny<Guid?>()))
            .ReturnsAsync(new ResolutionResult(
                modelId,
                new CanonicalModelId(canonicalId, canonicalId),
                new List<ProviderConfig> { new() { Key = "test-provider" } }));
        return mock;
    }

    /// <summary>
    /// Creates a mock IHealthStore that returns healthy status for all providers.
    /// </summary>
    protected Mock<IHealthStore> CreateMockHealthStore(bool defaultHealthy = true)
    {
        var mock = new Mock<IHealthStore>();
        mock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultHealthy);
        mock.Setup(x => x.MarkFailureAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mock.Setup(x => x.MarkSuccessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    /// <summary>
    /// Creates a mock IQuotaTracker with unlimited quota.
    /// </summary>
    protected Mock<IQuotaTracker> CreateMockQuotaTracker()
    {
        var mock = new Mock<IQuotaTracker>();
        mock.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mock.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mock.Setup(x => x.RecordUsageAsync(
            It.IsAny<string>(), 
            It.IsAny<long>(), 
            It.IsAny<long>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    /// <summary>
    /// Creates a mock ICostService that returns zero cost.
    /// </summary>
    protected Mock<ICostService> CreateMockCostService()
    {
        var mock = new Mock<ICostService>();
        mock.Setup(x => x.GetCostAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModelCost?)null);
        return mock;
    }

    /// <summary>
    /// Creates a mock IRoutingScoreCalculator that returns a default score.
    /// </summary>
    protected Mock<IRoutingScoreCalculator> CreateMockRoutingScoreCalculator()
    {
        var mock = new Mock<IRoutingScoreCalculator>();
        mock.Setup(x => x.CalculateScore(
            It.IsAny<EnrichedCandidate>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .Returns(100.0);
        mock.Setup(x => x.GetEffectiveConfigurationAsync(
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoutingScoreConfiguration(
                weights: new RoutingScoreWeights(
                    qualityScoreWeight: 0.3,
                    quotaRemainingWeight: 0.3,
                    rateLimitSafetyWeight: 0.2,
                    latencyScoreWeight: 0.2
                ),
                freeTierBonusPoints: 50,
                minScoreThreshold: 0.0,
                preferFreeByDefault: true
            ));
        return mock;
    }
}
