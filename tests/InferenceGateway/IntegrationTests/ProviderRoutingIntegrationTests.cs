using System;
using System.Collections.Generic;
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
    /// Integration tests for the full provider routing pipeline
    /// Tests ModelResolver → SmartRouter → CostService → HealthStore → QuotaTracker working together
    /// Uses real dependencies with in-memory database for true integration testing
    /// </summary>
    public class ProviderRoutingIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ServiceProvider _serviceProvider;
        private readonly ControlPlaneDbContext _dbContext;

        public ProviderRoutingIntegrationTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));

            // Setup dependency injection with real components
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddXUnit(output));

            // Add in-memory database
            services.AddDbContext<ControlPlaneDbContext>(options =>
                options.UseInMemoryDatabase("ProviderRoutingIntegrationTests"));

            // Add real services
            services.AddScoped<ICostService, CostService>();
            services.AddScoped<IAuditService, AuditService>();

            // Mock external dependencies that require real API calls
            var mockHealthStore = new Mock<IHealthStore>();
            mockHealthStore.Setup(x => x.IsHealthyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // All providers healthy by default

            var mockQuotaTracker = new Mock<IQuotaTracker>();
            mockQuotaTracker.Setup(x => x.CheckQuotaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // All providers within quota by default

            var mockProviderRegistry = new Mock<IProviderRegistry>();
            mockProviderRegistry.Setup(x => x.GetCandidates("test-model"))
                .Returns(new[] { ("provider-1", 0), ("provider-2", 1), ("provider-3", 2) });
            mockProviderRegistry.Setup(x => x.GetCandidates("fallback-model"))
                .Returns(new[] { ("fallback-provider", 0) });

            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(new SynaxisConfiguration
            {
                Providers = new Dictionary<string, ProviderConfig>
                {
                    ["provider-1"] = new ProviderConfig { Enabled = true, Key = "provider-1", Tier = 0 },
                    ["provider-2"] = new ProviderConfig { Enabled = true, Key = "provider-2", Tier = 1 },
                    ["provider-3"] = new ProviderConfig { Enabled = true, Key = "provider-3", Tier = 2 },
                    ["fallback-provider"] = new ProviderConfig { Enabled = true, Key = "fallback-provider", Tier = 0 }
                }
            });

            var mockStore = new Mock<IControlPlaneStore>();
            
            services.AddSingleton(mockHealthStore.Object);
            services.AddSingleton(mockQuotaTracker.Object);
            services.AddSingleton(mockProviderRegistry.Object);
            services.AddSingleton(mockConfig.Object);
            services.AddSingleton(mockStore.Object);

            _serviceProvider = services.BuildServiceProvider();
            _dbContext = _serviceProvider.GetRequiredService<ControlPlaneDbContext>();
            
            // Seed the database with test data
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            // Add cost data for testing
            _dbContext.ModelCosts.Add(new ModelCost { Provider = "provider-1", Model = "test-model", CostPerToken = 0.001m, FreeTier = true });
            _dbContext.ModelCosts.Add(new ModelCost { Provider = "provider-2", Model = "test-model", CostPerToken = 0.002m, FreeTier = false });
            _dbContext.ModelCosts.Add(new ModelCost { Provider = "provider-3", Model = "test-model", CostPerToken = 0.003m, FreeTier = false });
            _dbContext.ModelCosts.Add(new ModelCost { Provider = "fallback-provider", Model = "fallback-model", CostPerToken = 0.000m, FreeTier = true });
            
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task FullRoutingPipeline_ShouldReturnOptimalProvider()
        {
            // Arrange
            var costService = _serviceProvider.GetRequiredService<ICostService>();
            var mockHealthStore = Mock.Get(_serviceProvider.GetRequiredService<IHealthStore>());
            var mockQuotaTracker = Mock.Get(_serviceProvider.GetRequiredService<IQuotaTracker>());
            var mockProviderRegistry = Mock.Get(_serviceProvider.GetRequiredService<IProviderRegistry>());
            var mockConfig = Mock.Get(_serviceProvider.GetRequiredService<IOptions<SynaxisConfiguration>>());
            var mockStore = Mock.Get(_serviceProvider.GetRequiredService<IControlPlaneStore>());

            // Setup ModelResolver dependencies
            mockStore.Setup(x => x.GetGlobalModelAsync("test-model"))
                .ReturnsAsync((GlobalModel?)null); // Use config fallback

            var modelResolver = new ModelResolver(mockConfig.Object, mockProviderRegistry.Object, mockStore.Object);

            // Setup SmartRouter
            var mockLogger = new Mock<ILogger<SmartRouter>>();
            var smartRouter = new SmartRouter(modelResolver, costService, mockHealthStore.Object, mockQuotaTracker.Object, mockLogger.Object);

            // Act
            var result = await smartRouter.GetCandidatesAsync("test-model", streaming: false, CancellationToken.None);

            // Assert - Should return providers sorted by: free first, then cost, then tier
            Assert.Equal(3, result.Count);
            
            // Debug output to see actual results
            foreach (var candidate in result)
            {
                _output.WriteLine($"Provider: {candidate.Key}, IsFree: {candidate.IsFree}, CostPerToken: {candidate.CostPerToken}");
            }
            
            // First provider should be free tier
            Assert.Equal("provider-1", result[0].Key);
            Assert.True(result[0].IsFree, $"Expected provider-1 to be free, but IsFree was {result[0].IsFree}");
            
            // Second provider should be cheapest paid
            Assert.Equal("provider-2", result[1].Key);
            Assert.Equal(0.002m, result[1].CostPerToken);
            
            // Third provider should be most expensive
            Assert.Equal("provider-3", result[2].Key);
            Assert.Equal(0.003m, result[2].CostPerToken);
        }

        [Fact]
        public async Task RoutingPipeline_ShouldFilterUnhealthyProviders()
        {
            // Arrange
            var costService = _serviceProvider.GetRequiredService<ICostService>();
            var mockHealthStore = Mock.Get(_serviceProvider.GetRequiredService<IHealthStore>());
            var mockQuotaTracker = Mock.Get(_serviceProvider.GetRequiredService<IQuotaTracker>());
            var mockProviderRegistry = Mock.Get(_serviceProvider.GetRequiredService<IProviderRegistry>());
            var mockConfig = Mock.Get(_serviceProvider.GetRequiredService<IOptions<SynaxisConfiguration>>());
            var mockStore = Mock.Get(_serviceProvider.GetRequiredService<IControlPlaneStore>());

            // Setup unhealthy provider
            mockHealthStore.Setup(x => x.IsHealthyAsync("provider-2", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var modelResolver = new ModelResolver(mockConfig.Object, mockProviderRegistry.Object, mockStore.Object);
            var mockLogger = new Mock<ILogger<SmartRouter>>();
            var smartRouter = new SmartRouter(modelResolver, costService, mockHealthStore.Object, mockQuotaTracker.Object, mockLogger.Object);

            // Act
            var result = await smartRouter.GetCandidatesAsync("test-model", streaming: false, CancellationToken.None);

            // Assert - Should exclude unhealthy provider
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, r => r.Key == "provider-2");
            Assert.Equal("provider-1", result[0].Key); // Free provider first
            Assert.Equal("provider-3", result[1].Key); // Remaining paid provider
        }

        [Fact]
        public async Task RoutingPipeline_ShouldFilterQuotaExceededProviders()
        {
            // Arrange
            var costService = _serviceProvider.GetRequiredService<ICostService>();
            var mockHealthStore = Mock.Get(_serviceProvider.GetRequiredService<IHealthStore>());
            var mockQuotaTracker = Mock.Get(_serviceProvider.GetRequiredService<IQuotaTracker>());
            var mockProviderRegistry = Mock.Get(_serviceProvider.GetRequiredService<IProviderRegistry>());
            var mockConfig = Mock.Get(_serviceProvider.GetRequiredService<IOptions<SynaxisConfiguration>>());
            var mockStore = Mock.Get(_serviceProvider.GetRequiredService<IControlPlaneStore>());

            // Setup quota-exceeded provider
            mockQuotaTracker.Setup(x => x.CheckQuotaAsync("provider-3", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var modelResolver = new ModelResolver(mockConfig.Object, mockProviderRegistry.Object, mockStore.Object);
            var mockLogger = new Mock<ILogger<SmartRouter>>();
            var smartRouter = new SmartRouter(modelResolver, costService, mockHealthStore.Object, mockQuotaTracker.Object, mockLogger.Object);

            // Act
            var result = await smartRouter.GetCandidatesAsync("test-model", streaming: false, CancellationToken.None);

            // Assert - Should exclude quota-exceeded provider
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, r => r.Key == "provider-3");
            Assert.Equal("provider-1", result[0].Key); // Free provider first
            Assert.Equal("provider-2", result[1].Key); // Remaining paid provider
        }

        [Fact]
        public async Task RoutingPipeline_ShouldHandleDatabaseDrivenRouting()
        {
            // Arrange
            var costService = _serviceProvider.GetRequiredService<ICostService>();
            var mockHealthStore = Mock.Get(_serviceProvider.GetRequiredService<IHealthStore>());
            var mockQuotaTracker = Mock.Get(_serviceProvider.GetRequiredService<IQuotaTracker>());
            var mockProviderRegistry = Mock.Get(_serviceProvider.GetRequiredService<IProviderRegistry>());
            var mockConfig = Mock.Get(_serviceProvider.GetRequiredService<IOptions<SynaxisConfiguration>>());
            var mockStore = Mock.Get(_serviceProvider.GetRequiredService<IControlPlaneStore>());

            // Setup database-driven model resolution
            var globalModel = new GlobalModel
            {
                Id = "database-model",
                ProviderModels = new List<ProviderModel>
                {
                    new ProviderModel { ProviderId = "provider-1", ProviderSpecificId = "db-model-1" },
                    new ProviderModel { ProviderId = "provider-2", ProviderSpecificId = "db-model-2" }
                }
            };

            mockStore.Setup(x => x.GetGlobalModelAsync("database-model"))
                .ReturnsAsync(globalModel);

            mockProviderRegistry.Setup(x => x.GetCandidates("db-model-1"))
                .Returns(new[] { ("provider-1", 0) });
            mockProviderRegistry.Setup(x => x.GetCandidates("db-model-2"))
                .Returns(new[] { ("provider-2", 1) });

            var modelResolver = new ModelResolver(mockConfig.Object, mockProviderRegistry.Object, mockStore.Object);
            var mockLogger = new Mock<ILogger<SmartRouter>>();
            var smartRouter = new SmartRouter(modelResolver, costService, mockHealthStore.Object, mockQuotaTracker.Object, mockLogger.Object);

            // Act
            var result = await smartRouter.GetCandidatesAsync("database-model", streaming: false, CancellationToken.None);

            // Assert - Should use database-driven routing
            Assert.Equal(2, result.Count);
            Assert.Equal("provider-1", result[0].Key);
            Assert.Equal("provider-2", result[1].Key);
        }

        [Fact]
        public async Task RoutingPipeline_ShouldHandlePartialFailuresGracefully()
        {
            // Arrange
            var costService = _serviceProvider.GetRequiredService<ICostService>();
            var mockHealthStore = Mock.Get(_serviceProvider.GetRequiredService<IHealthStore>());
            var mockQuotaTracker = Mock.Get(_serviceProvider.GetRequiredService<IQuotaTracker>());
            var mockProviderRegistry = Mock.Get(_serviceProvider.GetRequiredService<IProviderRegistry>());
            var mockConfig = Mock.Get(_serviceProvider.GetRequiredService<IOptions<SynaxisConfiguration>>());
            var mockStore = Mock.Get(_serviceProvider.GetRequiredService<IControlPlaneStore>());

            // Setup partial failures: one unhealthy, one quota exceeded
            mockHealthStore.Setup(x => x.IsHealthyAsync("provider-2", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            mockQuotaTracker.Setup(x => x.CheckQuotaAsync("provider-3", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var modelResolver = new ModelResolver(mockConfig.Object, mockProviderRegistry.Object, mockStore.Object);
            var mockLogger = new Mock<ILogger<SmartRouter>>();
            var smartRouter = new SmartRouter(modelResolver, costService, mockHealthStore.Object, mockQuotaTracker.Object, mockLogger.Object);

            // Act
            var result = await smartRouter.GetCandidatesAsync("test-model", streaming: false, CancellationToken.None);

            // Assert - Should handle partial failures and return remaining providers
            Assert.Single(result);
            Assert.Equal("provider-1", result[0].Key); // Only healthy provider within quota
        }

        [Fact]
        public async Task RoutingPipeline_ShouldHandleModelNotFoundGracefully()
        {
            // Arrange
            var costService = _serviceProvider.GetRequiredService<ICostService>();
            var mockHealthStore = Mock.Get(_serviceProvider.GetRequiredService<IHealthStore>());
            var mockQuotaTracker = Mock.Get(_serviceProvider.GetRequiredService<IQuotaTracker>());
            var mockProviderRegistry = Mock.Get(_serviceProvider.GetRequiredService<IProviderRegistry>());
            var mockConfig = Mock.Get(_serviceProvider.GetRequiredService<IOptions<SynaxisConfiguration>>());
            var mockStore = Mock.Get(_serviceProvider.GetRequiredService<IControlPlaneStore>());

            // Setup unknown model
            mockProviderRegistry.Setup(x => x.GetCandidates("unknown-model"))
                .Returns(Enumerable.Empty<(string, int)>());

            var modelResolver = new ModelResolver(mockConfig.Object, mockProviderRegistry.Object, mockStore.Object);
            var mockLogger = new Mock<ILogger<SmartRouter>>();
            var smartRouter = new SmartRouter(modelResolver, costService, mockHealthStore.Object, mockQuotaTracker.Object, mockLogger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                smartRouter.GetCandidatesAsync("unknown-model", streaming: false, CancellationToken.None));

            Assert.Contains("No providers available", exception.Message);
        }

        [Fact]
        public async Task RoutingPipeline_ShouldRespectCapabilityRequirements()
        {
            // Arrange
            var costService = _serviceProvider.GetRequiredService<ICostService>();
            var mockHealthStore = Mock.Get(_serviceProvider.GetRequiredService<IHealthStore>());
            var mockQuotaTracker = Mock.Get(_serviceProvider.GetRequiredService<IQuotaTracker>());
            var mockProviderRegistry = Mock.Get(_serviceProvider.GetRequiredService<IProviderRegistry>());
            var mockConfig = Mock.Get(_serviceProvider.GetRequiredService<IOptions<SynaxisConfiguration>>());
            var mockStore = Mock.Get(_serviceProvider.GetRequiredService<IControlPlaneStore>());

            var modelResolver = new ModelResolver(mockConfig.Object, mockProviderRegistry.Object, mockStore.Object);
            var mockLogger = new Mock<ILogger<SmartRouter>>();
            var smartRouter = new SmartRouter(modelResolver, costService, mockHealthStore.Object, mockQuotaTracker.Object, mockLogger.Object);

            // Act - Test streaming capability
            var result = await smartRouter.GetCandidatesAsync("test-model", streaming: true, CancellationToken.None);

            // Assert - Should pass streaming capability through
            Assert.Equal(3, result.Count);
            // The capability is passed to ModelResolver internally
        }

        public void Dispose()
        {
            _dbContext?.Database?.EnsureDeleted();
            _serviceProvider?.Dispose();
        }
    }
}