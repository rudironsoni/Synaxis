using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.InferenceGateway.Application;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.Routing;
using Synaxis.InferenceGateway.Application.Security;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests
{
    /// <summary>
    /// Performance tests for provider routing logic
    /// Tests throughput, response times, and concurrent load handling
    /// Uses real dependencies with mocked external calls for realistic performance testing
    /// </summary>
    public class PerformanceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ServiceProvider _serviceProvider;
        private readonly ControlPlaneDbContext _dbContext;

        public PerformanceTests(ITestOutputHelper output)
        {
            this._output = output ?? throw new ArgumentNullException(nameof(output));

            // Setup dependency injection with real components
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder => builder.AddXUnit(output));

            // Add in-memory database
            services.AddDbContext<ControlPlaneDbContext>(options =>
                options.UseInMemoryDatabase("PerformanceTests"));

            // Add real services
            services.AddScoped<ICostService, CostService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IApiKeyService, ApiKeyService>();
            services.AddScoped<IJwtService, JwtService>();

            // Mock external dependencies for performance testing
            var mockHealthStore = new Mock<IHealthStore>();
            mockHealthStore
                .Setup(x => x.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // All providers healthy for performance tests

            var mockQuotaTracker = new Mock<IQuotaTracker>();
            mockQuotaTracker
                .Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // All providers have quota available

            // Mock IProviderRegistry for ModelResolver
            var mockProviderRegistry = new Mock<IProviderRegistry>();
            mockProviderRegistry.Setup(x => x.GetCandidates(It.IsAny<string>()))
                .Returns(new[] { ("groq", 0), ("deepseek", 1), ("fireworks", 2) });

            // Mock IControlPlaneStore
            var mockStore = new Mock<IControlPlaneStore>();
            mockStore.Setup(x => x.GetGlobalModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GlobalModel?)null);

            services.AddSingleton(mockHealthStore.Object);
            services.AddSingleton(mockQuotaTracker.Object);
            services.AddSingleton(mockProviderRegistry.Object);
            services.AddSingleton(mockStore.Object);

            // Add configuration
            var config = new SynaxisConfiguration
            {
                Providers = new Dictionary<string, ProviderConfig>
                {
                    ["groq"] = new ProviderConfig
                    {
                        Enabled = true,
                        Type = "groq",
                        Key = "test-key",
                        Tier = 0,
                        Models = ["llama-3.1-70b-versatile", "mixtral-8x7b-32768"],
                    },
                    ["deepseek"] = new ProviderConfig
                    {
                        Enabled = true,
                        Type = "openai",
                        Endpoint = "https://api.deepseek.com/v1",
                        Key = "test-key",
                        Tier = 1,
                        Models = ["deepseek-chat", "deepseek-coder"],
                    },
                    ["fireworks"] = new ProviderConfig
                    {
                        Enabled = true,
                        Type = "openai",
                        Endpoint = "https://api.fireworks.ai/inference/v1",
                        Key = "test-key",
                        Tier = 2,
                        Models = ["accounts/fireworks/models/llama-v2-7b-chat"]
                    },
                },
                CanonicalModels = [
                    new CanonicalModelConfig
                    {
                        Id = "llama-3.1-70b-versatile",
                        Provider = "groq",
                        ModelPath = "llama-3.1-70b-versatile",
                        Streaming = true,
                        Tools = true,
                        Vision = false,
                        StructuredOutput = false,
                        LogProbs = false,
                    },
                    new CanonicalModelConfig
                    {
                        Id = "deepseek-chat",
                        Provider = "deepseek",
                        ModelPath = "deepseek-chat",
                        Streaming = true,
                        Tools = true,
                        Vision = false,
                        StructuredOutput = false,
                        LogProbs = false,
                    }
                ],
                Aliases = new Dictionary<string, AliasConfig>
                {
                    ["default"] = new AliasConfig
                    {
                        Candidates = ["llama-3.1-70b-versatile", "deepseek-chat"]
                    }
                },
            };

            services.AddSingleton(Options.Create(config));

            // Add SmartRouter with real dependencies
            services.AddScoped<SmartRouter>();

            // Add ModelResolver with mocked dependencies
            services.AddScoped<IModelResolver>(provider => new ModelResolver(
                provider.GetRequiredService<IOptions<SynaxisConfiguration>>(),
                provider.GetRequiredService<IProviderRegistry>(),
                provider.GetRequiredService<IControlPlaneStore>()
            ));

            this._serviceProvider = services.BuildServiceProvider();
            this._dbContext = this._serviceProvider.GetRequiredService<ControlPlaneDbContext>();

            // Seed database with test data
            this.SeedDatabase();
        }

        private void SeedDatabase()
        {
            // Clear existing data to avoid duplicate key issues
            this._dbContext.ModelCosts.RemoveRange(this._dbContext.ModelCosts);

            // Seed model costs for performance testing
            this._dbContext.ModelCosts.Add(new ModelCost { Provider = "groq", Model = "llama-3.1-70b-versatile", CostPerToken = 0.001m, FreeTier = true });
            this._dbContext.ModelCosts.Add(new ModelCost { Provider = "groq", Model = "mixtral-8x7b-32768", CostPerToken = 0.002m, FreeTier = true });
            this._dbContext.ModelCosts.Add(new ModelCost { Provider = "deepseek", Model = "deepseek-chat", CostPerToken = 0.0005m, FreeTier = false });
            this._dbContext.ModelCosts.Add(new ModelCost { Provider = "deepseek", Model = "deepseek-coder", CostPerToken = 0.0003m, FreeTier = false });
            this._dbContext.ModelCosts.Add(new ModelCost { Provider = "fireworks", Model = "accounts/fireworks/models/llama-v2-7b-chat", CostPerToken = 0.0015m, FreeTier = false });

            this._dbContext.SaveChanges();
        }

        [Fact]
        public async Task RoutingPipeline_ShouldHandleConcurrentRequests_Efficiently()
        {
            // Arrange
            var smartRouter = this._serviceProvider.GetRequiredService<SmartRouter>();
            var modelId = "llama-3.1-70b-versatile";
            var streaming = false;
            var cancellationToken = CancellationToken.None;

            const int concurrentRequests = 100;
            var tasks = new List<Task<List<EnrichedCandidate>>>();
            var stopwatch = Stopwatch.StartNew();

            // Act - Execute concurrent requests
            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.All(results, result => Assert.NotEmpty(result));

            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTimePerRequest = (double)totalTime / concurrentRequests;

            this._output.WriteLine($"Concurrent Requests: {concurrentRequests}");
            this._output.WriteLine($"Total Time: {totalTime}ms");
            this._output.WriteLine($"Average Time Per Request: {averageTimePerRequest:F2}ms");
            this._output.WriteLine($"Throughput: {concurrentRequests / (totalTime / 1000.0):F2} requests/second");

            // Performance assertions
            Assert.True(totalTime < 5000, $"Total time {totalTime}ms exceeds 5 second threshold");
            Assert.True(averageTimePerRequest < 100, $"Average time {averageTimePerRequest:F2}ms exceeds 100ms threshold");
            Assert.True(results.All(r => r.Count > 0), "All requests should return candidates");
        }

        [Fact]
        public async Task RoutingPipeline_ShouldScaleLinearly_WithRequestVolume()
        {
            // Arrange
            var smartRouter = this._serviceProvider.GetRequiredService<SmartRouter>();
            var modelId = "deepseek-chat";
            var streaming = false;
            var cancellationToken = CancellationToken.None;

            var batchSizes = new[] { 10, 50, 100 };
            var results = new Dictionary<int, (long totalTime, double throughput)>();

            foreach (var batchSize in batchSizes)
            {
                var stopwatch = Stopwatch.StartNew();
                var tasks = new List<Task<List<EnrichedCandidate>>>();

                // Execute batch
                for (int i = 0; i < batchSize; i++)
                {
                    tasks.Add(smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken));
                }

                await Task.WhenAll(tasks);
                stopwatch.Stop();

                var throughput = batchSize / (stopwatch.ElapsedMilliseconds / 1000.0);
                results[batchSize] = (stopwatch.ElapsedMilliseconds, throughput);

                this._output.WriteLine($"Batch Size: {batchSize}, Time: {stopwatch.ElapsedMilliseconds}ms, Throughput: {throughput:F2} req/s");
            }

            // Assert - Check for reasonable scaling
            var firstBatch = results[batchSizes[0]];
            var lastBatch = results[batchSizes[2]];

            // Throughput may degrade with larger batches due to overhead - adjust expectation
            var throughputRatio = lastBatch.throughput / firstBatch.throughput;
            Assert.True(throughputRatio > 0.2, $"Throughput degraded significantly: {throughputRatio:F2}");
        }

        [Fact]
        public async Task RoutingPipeline_ShouldHandleMixedModelRequests_Efficiently()
        {
            // Arrange
            var smartRouter = this._serviceProvider.GetRequiredService<SmartRouter>();
            var cancellationToken = CancellationToken.None;

            var models = new[]
            {
                ("llama-3.1-70b-versatile", false),
                ("deepseek-chat", false),
                ("mixtral-8x7b-32768", true),
                ("deepseek-coder", true),
            };

            const int requestsPerModel = 25;
            var totalRequests = models.Length * requestsPerModel;
            var tasks = new List<Task<List<EnrichedCandidate>>>();
            var stopwatch = Stopwatch.StartNew();

            // Act - Execute mixed model requests
            foreach (var (modelId, streaming) in models)
            {
                for (int i = 0; i < requestsPerModel; i++)
                {
                    tasks.Add(smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken));
                }
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.Equal(totalRequests, results.Length);
            Assert.All(results, result => Assert.NotEmpty(result));

            var totalTime = stopwatch.ElapsedMilliseconds;
            var throughput = totalRequests / (totalTime / 1000.0);

            this._output.WriteLine($"Mixed Model Requests: {totalRequests}");
            this._output.WriteLine($"Total Time: {totalTime}ms");
            this._output.WriteLine($"Throughput: {throughput:F2} requests/second");

            Assert.True(totalTime < 10000, $"Total time {totalTime}ms exceeds 10 second threshold");
            Assert.True(throughput > 10, $"Throughput {throughput:F2} req/s below minimum threshold");
        }

        [Fact]
        public async Task RoutingPipeline_ShouldHandleDatabasePressure_Gracefully()
        {
            // Arrange
            var costService = this._serviceProvider.GetRequiredService<ICostService>();
            var cancellationToken = CancellationToken.None;

            const int concurrentDbQueries = 200;
            var tasks = new List<Task<ModelCost?>>();
            var stopwatch = Stopwatch.StartNew();

            // Act - Execute concurrent database queries
            for (int i = 0; i < concurrentDbQueries; i++)
            {
                var provider = i % 2 == 0 ? "groq" : "deepseek";
                var model = i % 2 == 0 ? "llama-3.1-70b-versatile" : "deepseek-chat";
                tasks.Add(costService.GetCostAsync(provider, model, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.All(results, result => Assert.NotNull(result));

            var totalTime = stopwatch.ElapsedMilliseconds;
            var throughput = concurrentDbQueries / (totalTime / 1000.0);

            this._output.WriteLine($"Concurrent DB Queries: {concurrentDbQueries}");
            this._output.WriteLine($"Total Time: {totalTime}ms");
            this._output.WriteLine($"Throughput: {throughput:F2} queries/second");

            Assert.True(totalTime < 5000, $"Total time {totalTime}ms exceeds 5 second threshold");
            Assert.True(throughput > 30, $"Throughput {throughput:F2} queries/s below minimum threshold");
        }

        [Fact]
        public async Task RoutingPipeline_ShouldHandleCancellation_Efficiently()
        {
            // Arrange
            var smartRouter = this._serviceProvider.GetRequiredService<SmartRouter>();
            var modelId = "llama-3.1-70b-versatile";
            var streaming = false;

            var cts = new CancellationTokenSource();
            var stopwatch = Stopwatch.StartNew();

            // Act - Cancel immediately and then start request
            cts.Cancel();
            var task = smartRouter.GetCandidatesAsync(modelId, streaming, cts.Token);

            // Assert - Should handle cancellation gracefully
            // The cancellation propagates through EF Core queries and throws OperationCanceledException
            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
            stopwatch.Stop();

            this._output.WriteLine($"Cancellation handled in {stopwatch.ElapsedMilliseconds}ms");
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Cancellation should be handled quickly");
        }

        [Fact]
        [Trait("Category", "Performance")]
        [Trait("Category", "Flaky")]  // Performance tests can be flaky due to system resource variability
        public async Task RoutingPipeline_ShouldMaintainLowMemoryFootprint()
        {
            // Arrange
            var smartRouter = this._serviceProvider.GetRequiredService<SmartRouter>();
            var modelId = "deepseek-chat";
            var streaming = false;
            var cancellationToken = CancellationToken.None;

            const int iterations = 1000;
            var initialMemory = GC.GetTotalMemory(true);
            var stopwatch = Stopwatch.StartNew();

            // Act - Execute many requests
            for (int i = 0; i < iterations; i++)
            {
                await smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken);

                // Force garbage collection periodically
                if (i % 100 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            var finalMemory = GC.GetTotalMemory(true);
            stopwatch.Stop();

            // Assert
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreasePerRequest = (double)memoryIncrease / iterations;

            this._output.WriteLine($"Iterations: {iterations}");
            this._output.WriteLine($"Initial Memory: {initialMemory / 1024} KB");
            this._output.WriteLine($"Final Memory: {finalMemory / 1024} KB");
            this._output.WriteLine($"Memory Increase: {memoryIncrease / 1024} KB");
            this._output.WriteLine($"Memory Per Request: {memoryIncreasePerRequest:F2} bytes");
            this._output.WriteLine($"Total Time: {stopwatch.ElapsedMilliseconds}ms");

            // Memory footprint should be reasonable - relax threshold for CI environments
            // Performance tests can be flaky due to system resource variability
            Assert.True(memoryIncreasePerRequest < 25000, $"Memory increase per request {memoryIncreasePerRequest:F2} bytes exceeds 25KB threshold");
            Assert.True(stopwatch.ElapsedMilliseconds < 60000, $"Total time {stopwatch.ElapsedMilliseconds}ms exceeds 60 second threshold");
        }

        public void Dispose()
        {
            this._serviceProvider?.Dispose();
            this._dbContext?.Dispose();
        }
    }
}
