// <copyright file="SmartRouterTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Infrastructure.Routing;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests
{
    /// <summary>
    /// Unit tests for SmartRouter - core routing logic
    /// Tests provider selection, cost optimization, health filtering, and quota enforcement.
    /// </summary>
    public class SmartRouterTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<IModelResolver> _mockModelResolver;
        private readonly Mock<ICostService> _mockCostService;
        private readonly Mock<IHealthStore> _mockHealthStore;
        private readonly Mock<IQuotaTracker> _mockQuotaTracker;
        private readonly Mock<IRoutingScoreCalculator> _mockRoutingScoreCalculator;
        private readonly Mock<ILogger<SmartRouter>> _mockLogger;
        private readonly SmartRouter _smartRouter;

        public SmartRouterTests(ITestOutputHelper output)
        {
            this._output = output ?? throw new ArgumentNullException(nameof(output));
            this._mockModelResolver = new Mock<IModelResolver>();
            this._mockCostService = new Mock<ICostService>();
            this._mockHealthStore = new Mock<IHealthStore>();
            this._mockQuotaTracker = new Mock<IQuotaTracker>();
            this._mockRoutingScoreCalculator = new Mock<IRoutingScoreCalculator>();
            this._mockLogger = new Mock<ILogger<SmartRouter>>();

            this._smartRouter = new SmartRouter(
                this._mockModelResolver.Object,
                this._mockCostService.Object,
                this._mockHealthStore.Object,
                this._mockQuotaTracker.Object,
                this._mockRoutingScoreCalculator.Object,
                this._mockLogger.Object);
        }

        [Fact]
        public async Task GetCandidatesAsync_ShouldReturnEmptyList_WhenNoProvidersFound()
        {
            // Arrange
            var modelId = "unknown-model";
            var streaming = false;
            var cancellationToken = CancellationToken.None;

            var candidates = new List<ProviderConfig>
            {
                new ProviderConfig { Key = "healthy-provider", Tier = 0 },
                new ProviderConfig { Key = "unhealthy-provider", Tier = 1 },
            };

            var resolutionResult = new ResolutionResult(
                modelId,
                new CanonicalModelId(modelId, modelId),
                candidates);

            this._mockModelResolver
                .Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>()))
                .ReturnsAsync(resolutionResult);

            this._mockHealthStore
                .Setup(x => x.IsHealthyAsync("healthy-provider", cancellationToken))
                .ReturnsAsync(true);
            this._mockHealthStore
                .Setup(x => x.IsHealthyAsync("unhealthy-provider", cancellationToken))
                .ReturnsAsync(false);

            this._mockQuotaTracker
                .Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(true);

            this._mockCostService
                .Setup(x => x.GetCostAsync(It.IsAny<string>(), modelId, cancellationToken))
                .ReturnsAsync(new ModelCost { Provider = "healthy-provider", Model = modelId, CostPerToken = 0.001m });

            // Act
            var result = await this._smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken);

            // Assert
            Assert.Single(result);
            Assert.Equal("healthy-provider", result[0].Key);
            this._mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping unhealthy provider 'unhealthy-provider'")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetCandidatesAsync_ShouldFilterQuotaExceededProviders()
        {
            // Arrange
            var modelId = "test-model";
            var streaming = false;
            var cancellationToken = CancellationToken.None;

            var candidates = new List<ProviderConfig>
            {
                new ProviderConfig { Key = "within-quota-provider", Tier = 0 },
                new ProviderConfig { Key = "exceeded-quota-provider", Tier = 1 },
            };

            var resolutionResult = new ResolutionResult(
                modelId,
                new CanonicalModelId(modelId, modelId),
                candidates);

            this._mockModelResolver
                .Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>()))
                .ReturnsAsync(resolutionResult);

            this._mockHealthStore
                .Setup(x => x.IsHealthyAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(true);

            this._mockQuotaTracker
                .Setup(x => x.CheckQuotaAsync("within-quota-provider", cancellationToken))
                .ReturnsAsync(true);
            this._mockQuotaTracker
                .Setup(x => x.CheckQuotaAsync("exceeded-quota-provider", cancellationToken))
                .ReturnsAsync(false);

            this._mockCostService
                .Setup(x => x.GetCostAsync(It.IsAny<string>(), modelId, cancellationToken))
                .ReturnsAsync(new ModelCost { Provider = "within-quota-provider", Model = modelId, CostPerToken = 0.001m });

            // Act
            var result = await this._smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken);

            // Assert
            Assert.Single(result);
            Assert.Equal("within-quota-provider", result[0].Key);
            this._mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping quota-exceeded provider 'exceeded-quota-provider'")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
#pragma warning disable MA0051
        public async Task GetCandidatesAsync_ShouldSortByPriority_FreeFirst_ThenCost_ThenTier()
        {
            // Arrange
            var modelId = "test-model";
            var streaming = false;
            var cancellationToken = CancellationToken.None;

            var candidates = new List<ProviderConfig>
            {
                new ProviderConfig { Key = "paid-tier1", Tier = 1 },
                new ProviderConfig { Key = "free-tier2", Tier = 2 },
                new ProviderConfig { Key = "free-tier1", Tier = 1 },
                new ProviderConfig { Key = "paid-tier2", Tier = 2 },
            };

            var resolutionResult = new ResolutionResult(
                modelId,
                new CanonicalModelId(modelId, modelId),
                candidates);

            this._mockModelResolver
                .Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>()))
                .ReturnsAsync(resolutionResult);

            this._mockHealthStore
                .Setup(x => x.IsHealthyAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(true);
            this._mockQuotaTracker
                .Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(true);

            this._mockCostService
                .Setup(x => x.GetCostAsync("paid-tier1", modelId, cancellationToken))
                .ReturnsAsync(new ModelCost { CostPerToken = 0.002m, FreeTier = false });
            this._mockCostService
                .Setup(x => x.GetCostAsync("free-tier2", modelId, cancellationToken))
                .ReturnsAsync(new ModelCost { CostPerToken = 0.000m, FreeTier = true });
            this._mockCostService
                .Setup(x => x.GetCostAsync("free-tier1", modelId, cancellationToken))
                .ReturnsAsync(new ModelCost { CostPerToken = 0.000m, FreeTier = true });
            this._mockCostService
                .Setup(x => x.GetCostAsync("paid-tier2", modelId, cancellationToken))
                .ReturnsAsync(new ModelCost { CostPerToken = 0.001m, FreeTier = false });

            // Mock routing score calculator to return scores that follow the expected order:
            // free-tier1 (highest), free-tier2, paid-tier2 (lower cost), paid-tier1 (highest cost, lowest score)
            this._mockRoutingScoreCalculator
                .Setup(x => x.CalculateScore(It.Is<EnrichedCandidate>(c => c.Key == "free-tier1"), null, null))
                .Returns(100.0); // Free + tier 1
            this._mockRoutingScoreCalculator
                .Setup(x => x.CalculateScore(It.Is<EnrichedCandidate>(c => c.Key == "free-tier2"), null, null))
                .Returns(90.0); // Free + tier 2
            this._mockRoutingScoreCalculator
                .Setup(x => x.CalculateScore(It.Is<EnrichedCandidate>(c => c.Key == "paid-tier2"), null, null))
                .Returns(50.0); // Paid + lower cost
            this._mockRoutingScoreCalculator
                .Setup(x => x.CalculateScore(It.Is<EnrichedCandidate>(c => c.Key == "paid-tier1"), null, null))
                .Returns(40.0); // Paid + higher cost

            // Act
            var result = await this._smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken);

            // Assert - should be sorted: free first (by cost), then paid (by cost), then by tier
            Assert.Equal(4, result.Count);

            // First two should be free providers
            Assert.True(result[0].IsFree);
            Assert.True(result[1].IsFree);

            // Within free providers, ordered by tier
            Assert.Equal(1, result[0].Config.Tier);
            Assert.Equal(2, result[1].Config.Tier);

            // Last two should be paid providers
            Assert.False(result[2].IsFree);
            Assert.False(result[3].IsFree);

            // Paid providers ordered by cost
            Assert.Equal(0.001m, result[2].CostPerToken);
            Assert.Equal(0.002m, result[3].CostPerToken);
        }
#pragma warning restore MA0051

        [Fact]
        public async Task GetCandidatesAsync_ShouldPassThroughCapabilities()
        {
            // Arrange
            var modelId = "test-model";
            var streaming = true;
            var cancellationToken = CancellationToken.None;

            var resolutionResult = new ResolutionResult(
                modelId,
                new CanonicalModelId(modelId, modelId),
                new List<ProviderConfig>());

            this._mockModelResolver
                .Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>()))
                .ReturnsAsync(resolutionResult);

            // Act
            await Assert.ThrowsAsync<ArgumentException>(() =>
                this._smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken));

            // Assert - Verify that capabilities were passed through
            this._mockModelResolver.Verify(
                x => x.ResolveAsync(
                    modelId,
                    EndpointKind.ChatCompletions,
                    It.Is<RequiredCapabilities>(caps => caps.Streaming)),
                Times.Once);
        }

        [Fact]
        public async Task GetCandidatesAsync_ShouldHandleNullCostsGracefully()
        {
            // Arrange
            var modelId = "test-model";
            var streaming = false;
            var cancellationToken = CancellationToken.None;

            var candidates = new List<ProviderConfig>
            {
                new ProviderConfig { Key = "provider-without-cost", Tier = 0 },
            };

            var resolutionResult = new ResolutionResult(
                modelId,
                new CanonicalModelId(modelId, modelId),
                candidates);

            this._mockModelResolver
                .Setup(x => x.ResolveAsync(modelId, EndpointKind.ChatCompletions, It.IsAny<RequiredCapabilities>()))
                .ReturnsAsync(resolutionResult);

            this._mockHealthStore
                .Setup(x => x.IsHealthyAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(true);
            this._mockQuotaTracker
                .Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(true);
            this._mockCostService
                .Setup(x => x.GetCostAsync(It.IsAny<string>(), modelId, cancellationToken))
                .ReturnsAsync((ModelCost?)null);

            // Act
            var result = await this._smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken);

            // Assert
            Assert.Single(result);
            Assert.Equal("provider-without-cost", result[0].Key);
            Assert.False(result[0].IsFree); // Should default to false when cost is null
            Assert.Equal(decimal.MaxValue, result[0].CostPerToken); // Should use max value when cost is null
        }
    }
}
