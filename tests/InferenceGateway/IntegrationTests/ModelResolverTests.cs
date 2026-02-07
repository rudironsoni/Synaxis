using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.InferenceGateway.Application;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests
{
    public class ModelResolverTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<IOptions<SynaxisConfiguration>> _mockConfig;
        private readonly Mock<IProviderRegistry> _mockRegistry;
        private readonly Mock<IControlPlaneStore> _mockStore;

        public ModelResolverTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            _mockRegistry = new Mock<IProviderRegistry>();
            _mockStore = new Mock<IControlPlaneStore>();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_ForNullOptions()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ModelResolver(null!, _mockRegistry.Object, _mockStore.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_ForNullRegistry()
        {
            var config = CreateValidConfiguration();
            _mockConfig.Setup(x => x.Value).Returns(config);

            Assert.Throws<ArgumentNullException>(() =>
                new ModelResolver(_mockConfig.Object, null!, _mockStore.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_ForNullStore()
        {
            var config = CreateValidConfiguration();
            _mockConfig.Setup(x => x.Value).Returns(config);

            Assert.Throws<ArgumentNullException>(() =>
                new ModelResolver(_mockConfig.Object, _mockRegistry.Object, null!));
        }

        [Fact]
        public void Constructor_ShouldInitializeSuccessfully_WithValidDependencies()
        {
            var config = CreateValidConfiguration();
            _mockConfig.Setup(x => x.Value).Returns(config);

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            Assert.NotNull(resolver);
        }

        [Fact]
        public void Resolve_ShouldReturnValidResult_ForDirectModelMatch()
        {
            // Arrange
            var config = CreateValidConfiguration();
            _mockConfig.Setup(x => x.Value).Returns(config);

            _mockRegistry.Setup(r => r.GetCandidates("llama-3.3-70b-versatile"))
                .Returns(new[] { ("Groq", 0), ("DeepSeek", 1) });

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            // Act
            var result = resolver.Resolve("llama-3.3-70b-versatile");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("llama-3.3-70b-versatile", result.OriginalModelId);
            Assert.Equal(2, result.Candidates.Count);
            Assert.Equal("Groq", result.Candidates[0].Key);
            Assert.Equal("DeepSeek", result.Candidates[1].Key);
        }

        [Fact]
        public void Resolve_ShouldHandleModelAliases()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.Aliases = new Dictionary<string, AliasConfig>
            {
                ["default"] = new AliasConfig { Candidates = ["llama-3.3-70b-versatile", "deepseek-chat"] }
            };
            _mockConfig.Setup(x => x.Value).Returns(config);

            // Only first candidate should be used since it has providers
            _mockRegistry.Setup(r => r.GetCandidates("llama-3.3-70b-versatile"))
                .Returns(new[] { ("Groq", 0) });
            _mockRegistry.Setup(r => r.GetCandidates("deepseek-chat"))
                .Returns(new[] { ("DeepSeek", 1) });

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            // Act
            var result = resolver.Resolve("default");

            // Assert - Should only return providers from first candidate
            Assert.NotNull(result);
            Assert.Equal("default", result.OriginalModelId);
            Assert.Single(result.Candidates);
            Assert.Equal("Groq", result.Candidates[0].Key);
        }

        [Fact]
        public void Resolve_ShouldFilterByRequiredCapabilities()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.CanonicalModels = new List<CanonicalModelConfig>
            {
                new CanonicalModelConfig
                {
                    Id = "llama-3.3-70b-versatile",
                    Provider = "Groq",
                    ModelPath = "llama-3.3-70b-versatile",
                    Streaming = true,
                    Tools = false,
                    Vision = false,
                    StructuredOutput = false,
                    LogProbs = false
                }
            };
            _mockConfig.Setup(x => x.Value).Returns(config);

            _mockRegistry.Setup(r => r.GetCandidates("llama-3.3-70b-versatile"))
                .Returns(new[] { ("Groq", 0) });

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);
            var required = new RequiredCapabilities { Tools = true };

            // Act
            var result = resolver.Resolve("llama-3.3-70b-versatile", required);

            // Assert - Should return empty candidates since model doesn't support tools
            Assert.NotNull(result);
            Assert.Equal("llama-3.3-70b-versatile", result.OriginalModelId);
            Assert.Empty(result.Candidates);
        }

        [Fact]
        public async Task ResolveAsync_ShouldUseDatabaseGlobalModel_WhenAvailable()
        {
            // Arrange
            var config = CreateValidConfiguration();
            _mockConfig.Setup(x => x.Value).Returns(config);

            var globalModel = new GlobalModel
            {
                Id = "llama-3.3-70b-versatile",
                ProviderModels = new List<ProviderModel>
                {
                    new ProviderModel { ProviderId = "Groq", ProviderSpecificId = "llama-3.3-70b-versatile" },
                    new ProviderModel { ProviderId = "DeepSeek", ProviderSpecificId = "deepseek-chat" }
                }
            };
            _mockStore.Setup(s => s.GetGlobalModelAsync("llama-3.3-70b-versatile"))
                .ReturnsAsync(globalModel);

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            // Act
            var result = await resolver.ResolveAsync("llama-3.3-70b-versatile", EndpointKind.ChatCompletions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("llama-3.3-70b-versatile", result.OriginalModelId);
            Assert.Equal(2, result.Candidates.Count);
            Assert.Equal("Groq", result.Candidates[0].Key);
            Assert.Equal("DeepSeek", result.Candidates[1].Key);
        }

        [Fact]
        public async Task ResolveAsync_ShouldFallbackToConfig_WhenNoDatabaseModel()
        {
            // Arrange
            var config = CreateValidConfiguration();
            _mockConfig.Setup(x => x.Value).Returns(config);

            _mockStore.Setup(s => s.GetGlobalModelAsync("llama-3.3-70b-versatile"))
                .ReturnsAsync((GlobalModel?)null);
            _mockRegistry.Setup(r => r.GetCandidates("llama-3.3-70b-versatile"))
                .Returns(new[] { ("Groq", 0) });

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            // Act
            var result = await resolver.ResolveAsync("llama-3.3-70b-versatile", EndpointKind.ChatCompletions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("llama-3.3-70b-versatile", result.OriginalModelId);
            Assert.Single(result.Candidates);
            Assert.Equal("Groq", result.Candidates[0].Key);
        }

        [Fact]
        public async Task ResolveAsync_ShouldHandleTenantAliases()
        {
            // Arrange
            var config = CreateValidConfiguration();
            _mockConfig.Setup(x => x.Value).Returns(config);

            var tenantId = Guid.NewGuid();
            var alias = new ModelAlias { TargetModel = "llama-3.3-70b-versatile" };
            _mockStore.Setup(s => s.GetGlobalModelAsync("llama-3.3-70b-versatile"))
                .ReturnsAsync((GlobalModel?)null);
            _mockStore.Setup(s => s.GetAliasAsync(tenantId, "custom-alias"))
                .ReturnsAsync(alias);
            _mockRegistry.Setup(r => r.GetCandidates("llama-3.3-70b-versatile"))
                .Returns(new[] { ("Groq", 0) });

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            // Act
            var result = await resolver.ResolveAsync("custom-alias", EndpointKind.ChatCompletions, tenantId: tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("custom-alias", result.OriginalModelId);
            Assert.Single(result.Candidates);
            Assert.Equal("Groq", result.Candidates[0].Key);
        }

        [Fact]
        public async Task ResolveAsync_ShouldHandleModelCombos()
        {
            // Arrange
            var config = CreateValidConfiguration();
            // Add the combo models to providers
            config.Providers["Provider1"] = new ProviderConfig
            {
                Enabled = true,
                Key = "Provider1",
                Tier = 0,
                Models = ["model1"],
                Type = "openai"
            };
            config.Providers["Provider2"] = new ProviderConfig
            {
                Enabled = true,
                Key = "Provider2",
                Tier = 1,
                Models = ["model2"],
                Type = "openai"
            };
            _mockConfig.Setup(x => x.Value).Returns(config);

            var tenantId = Guid.NewGuid();
            var combo = new ModelCombo { OrderedModelsJson = "[\"model1\", \"model2\"]" };
            _mockStore.Setup(s => s.GetGlobalModelAsync("model1"))
                .ReturnsAsync((GlobalModel?)null);
            _mockStore.Setup(s => s.GetGlobalModelAsync("model2"))
                .ReturnsAsync((GlobalModel?)null);
            _mockStore.Setup(s => s.GetComboAsync(tenantId, "my-combo"))
                .ReturnsAsync(combo);
            // First candidate has providers, so resolver should stop there
            _mockRegistry.Setup(r => r.GetCandidates("model1"))
                .Returns(new[] { ("Provider1", 0) });
            _mockRegistry.Setup(r => r.GetCandidates("model2"))
                .Returns(new[] { ("Provider2", 1) });

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            // Act
            var result = await resolver.ResolveAsync("my-combo", EndpointKind.ChatCompletions, tenantId: tenantId);

            // Assert - Should only return providers from first candidate
            Assert.NotNull(result);
            Assert.Equal("my-combo", result.OriginalModelId);
            Assert.Single(result.Candidates);
            Assert.Equal("Provider1", result.Candidates[0].Key);
        }

        [Fact]
        public void Resolve_ShouldHandleFallbackToRawModelId_WhenCanonicalLookupFails()
        {
            // Arrange
            var config = CreateValidConfiguration();
            // Ensure no canonical config exists for this model
            config.CanonicalModels.Clear();
            // Add the complex model ID to a provider
            config.Providers["Provider"] = new ProviderConfig
            {
                Enabled = true,
                Key = "Provider",
                Tier = 0,
                Models = ["provider/model-name-with-slash"],
                Type = "openai"
            };
            _mockConfig.Setup(x => x.Value).Returns(config);

            // Simulate a model ID that contains '/' which might break parsing
            var complexModelId = "provider/model-name-with-slash";
            _mockRegistry.Setup(r => r.GetCandidates("model-name-with-slash"))
                .Returns(Enumerable.Empty<(string, int)>());
            _mockRegistry.Setup(r => r.GetCandidates(complexModelId))
                .Returns(new[] { ("Provider", 0) });

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            // Act
            var result = resolver.Resolve(complexModelId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(complexModelId, result.OriginalModelId);
            Assert.Single(result.Candidates);
            Assert.Equal("Provider", result.Candidates[0].Key);
        }

        [Fact]
        public void Resolve_ShouldReturnEmptyCandidates_WhenNoProvidersMatch()
        {
            // Arrange
            var config = CreateValidConfiguration();
            _mockConfig.Setup(x => x.Value).Returns(config);

            _mockRegistry.Setup(r => r.GetCandidates("non-existent-model"))
                .Returns(Enumerable.Empty<(string, int)>());

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            // Act
            var result = resolver.Resolve("non-existent-model");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("non-existent-model", result.OriginalModelId);
            Assert.Empty(result.Candidates);
        }

        [Fact]
        public void Resolve_ShouldFilterProvidersByCanonicalProvider_WhenSpecified()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.CanonicalModels = new List<CanonicalModelConfig>
            {
                new CanonicalModelConfig
                {
                    Id = "llama-3.3-70b-versatile",
                    Provider = "Groq",
                    ModelPath = "llama-3.3-70b-versatile"
                }
            };
            _mockConfig.Setup(x => x.Value).Returns(config);

            _mockRegistry.Setup(r => r.GetCandidates("llama-3.3-70b-versatile"))
                .Returns(new[] { ("Groq", 0), ("DeepSeek", 1) });

            var resolver = new ModelResolver(_mockConfig.Object, _mockRegistry.Object, _mockStore.Object);

            // Act
            var result = resolver.Resolve("llama-3.3-70b-versatile");

            // Assert - Should only include Groq since it matches the canonical provider
            Assert.NotNull(result);
            Assert.Equal("llama-3.3-70b-versatile", result.OriginalModelId);
            Assert.Single(result.Candidates);
            Assert.Equal("Groq", result.Candidates[0].Key);
        }

        private SynaxisConfiguration CreateValidConfiguration()
        {
            return new SynaxisConfiguration
            {
                Providers = new Dictionary<string, ProviderConfig>
                {
                    ["Groq"] = new ProviderConfig
                    {
                        Enabled = true,
                        Key = "Groq",
                        Tier = 0,
                        Models = ["llama-3.3-70b-versatile"],
                        Type = "groq"
                    },
                    ["DeepSeek"] = new ProviderConfig
                    {
                        Enabled = true,
                        Key = "DeepSeek",
                        Tier = 1,
                        Models = ["deepseek-chat"],
                        Type = "openai"
                    }
                },
                CanonicalModels = new List<CanonicalModelConfig>(),
                Aliases = new Dictionary<string, AliasConfig>()
            };
        }
    }
}
