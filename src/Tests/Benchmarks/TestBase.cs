// <copyright file="TestBase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Benchmarks;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Application;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;

/// <summary>
/// Base class for all Synaxis tests providing common mocking infrastructure and setup.
/// Provides factory methods for creating mock objects with sensible defaults.
/// </summary>
public abstract class TestBase
{
    /// <summary>
    /// Creates a mock ILogger for any type.
    /// </summary>
    /// <typeparam name="T">The type for which the logger is created.</typeparam>
    /// <returns>A mock logger instance.</returns>
    protected static Mock<ILogger<T>> CreateMockLogger<T>()
        where T : class
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
    /// <param name="responseText">The response text to return from the mock client.</param>
    /// <returns>A mock chat client instance.</returns>
    protected static Mock<IChatClient> CreateMockChatClient(string responseText = "Mock response")
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
    /// <param name="responseChunks">The response chunks to stream.</param>
    /// <returns>A mock streaming chat client instance.</returns>
    protected static Mock<IChatClient> CreateMockStreamingChatClient(params string[] responseChunks)
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
    /// <param name="providers">The provider configurations to use.</param>
    /// <returns>A mock provider registry instance.</returns>
    protected static Mock<IProviderRegistry> CreateMockProviderRegistry(IReadOnlyDictionary<string, ProviderConfig>? providers = null)
    {
        var mock = new Mock<IProviderRegistry>();

        providers ??= new Dictionary<string, ProviderConfig>(StringComparer.Ordinal)
        {
            ["groq"] = new ProviderConfig { Type = "groq", Tier = 0, Models = new List<string> { "llama-3.1-70b-versatile" } },
            ["openai"] = new ProviderConfig { Type = "openai", Tier = 1, Models = new List<string> { "gpt-4" } },
        };

        mock.Setup(x => x.GetProvider(It.IsAny<string>()))
            .Returns((string key) => providers.TryGetValue(key, out var provider) ? provider : null);

        mock.Setup(x => x.GetCandidates(It.IsAny<string>()))
            .Returns((string modelId) => providers
                .Where(p => p.Value.Models.Contains(modelId, StringComparer.Ordinal) || p.Value.Models.Contains("*", StringComparer.Ordinal))
                .Select(p => (p.Key, p.Value.Tier)));

        return mock;
    }

    /// <summary>
    /// Creates a mock IModelResolver that returns a default resolution.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="canonicalId">The canonical identifier.</param>
    /// <returns>A mock model resolver instance.</returns>
    protected static Mock<IModelResolver> CreateMockModelResolver(string modelId = "test-model", string canonicalId = "test-canonical")
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
    /// <param name="defaultHealthy">Whether providers should default to healthy.</param>
    /// <returns>A mock health store instance.</returns>
    protected static Mock<IHealthStore> CreateMockHealthStore(bool defaultHealthy = true)
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
    /// <returns>A mock quota tracker instance.</returns>
    protected static Mock<IQuotaTracker> CreateMockQuotaTracker()
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
    /// <returns>A mock cost service instance.</returns>
    protected static Mock<ICostService> CreateMockCostService()
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
    /// <returns>A mock routing score calculator instance.</returns>
    protected static Mock<IRoutingScoreCalculator> CreateMockRoutingScoreCalculator()
    {
        var mock = new Mock<IRoutingScoreCalculator>();
        mock.Setup(x => x.CalculateScoreAsync(
            It.IsAny<EnrichedCandidate>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(100.0);
        mock.Setup(x => x.GetEffectiveConfigurationAsync(
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoutingScoreConfiguration(
                Weights: new RoutingScoreWeights(
                    QualityScoreWeight: 0.3,
                    QuotaRemainingWeight: 0.3,
                    RateLimitSafetyWeight: 0.2,
                    LatencyScoreWeight: 0.2),
                FreeTierBonusPoints: 50,
                MinScoreThreshold: 0.0,
                PreferFreeByDefault: true));
        return mock;
    }
}
