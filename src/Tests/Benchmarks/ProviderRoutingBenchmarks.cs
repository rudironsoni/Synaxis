// <copyright file="ProviderRoutingBenchmarks.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.InferenceGateway.Application;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Application.Routing;

namespace Synaxis.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]

public class ProviderRoutingBenchmarks : TestBase
{
    private IModelResolver _modelResolver = null!;
    private ISmartRouter _smartRouter = null!;
    private IProviderRegistry _providerRegistry = null!;
    private IHealthStore _healthStore = null!;
    private IQuotaTracker _quotaTracker = null!;
    private ICostService _costService = null!;
    private IControlPlaneStore _controlPlaneStore = null!;
    private ILogger<ModelResolver> _modelResolverLogger = null!;
    private ILogger<SmartRouter> _smartRouterLogger = null!;
    private IRoutingScoreCalculator _routingScoreCalculator = null!;
    private IOptions<SynaxisConfiguration> _configOptions = null!;

    private const string SingleProviderModel = "llama-3.1-70b-versatile";
    private const string MultipleProviderModel = "gpt-4";
    private const string DefaultAlias = "default";

    [GlobalSetup]
    public void Setup()
    {
        this._modelResolverLogger = this.CreateMockLogger<ModelResolver>().Object;
        this._smartRouterLogger = this.CreateMockLogger<SmartRouter>().Object;
        this._healthStore = this.CreateMockHealthStore(true).Object;
        this._quotaTracker = this.CreateMockQuotaTracker().Object;
        this._costService = this.CreateMockCostService().Object;
        this._controlPlaneStore = CreateMockControlPlaneStore().Object;
        this._routingScoreCalculator = this.CreateMockRoutingScoreCalculator().Object;

        var config = this.CreateSynaxisConfiguration(13, 10, 10);
        this._configOptions = Options.Create(config);

        var providers = new ProviderRegistry(this._configOptions);
        this._providerRegistry = providers;

        this._modelResolver = new ModelResolver(
            this._configOptions,
            this._providerRegistry,
            this._controlPlaneStore);

        this._smartRouter = new SmartRouter(
            this._modelResolver,
            this._costService,
            this._healthStore,
            this._quotaTracker,
            this._routingScoreCalculator,
            this._smartRouterLogger);
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(13)]
    public async Task<ResolutionResult> ModelResolver_ResolveAsync_SingleCanonicalModel(int providerCount)
    {
        var config = this.CreateSynaxisConfiguration(providerCount, 1, 1);
        var configOptions = Options.Create(config);
        var providers = new ProviderRegistry(configOptions);
        var controlPlaneStore = CreateMockControlPlaneStore().Object;

        var resolver = new ModelResolver(
            configOptions,
            providers,
            controlPlaneStore);

        return await resolver.ResolveAsync(
            SingleProviderModel,
            EndpointKind.ChatCompletions,
            RequiredCapabilities.Default).ConfigureAwait(false);
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task<ResolutionResult> ModelResolver_ResolveAsync_MultipleCanonicalModels(int canonicalModelCount)
    {
        var config = this.CreateSynaxisConfiguration(13, canonicalModelCount, 5);
        var configOptions = Options.Create(config);
        var providers = new ProviderRegistry(configOptions);
        var controlPlaneStore = CreateMockControlPlaneStore().Object;

        var resolver = new ModelResolver(
            configOptions,
            providers,
            controlPlaneStore);

        return await resolver.ResolveAsync(
            MultipleProviderModel,
            EndpointKind.ChatCompletions,
            RequiredCapabilities.Default).ConfigureAwait(false);
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(13)]
    public async Task<IList<EnrichedCandidate>> SmartRouter_GetCandidatesAsync_SingleProvider(int providerCount)
    {
        var config = this.CreateSynaxisConfiguration(providerCount, 1, 1);
        var configOptions = Options.Create(config);
        var providers = new ProviderRegistry(configOptions);
        var controlPlaneStore = CreateMockControlPlaneStore().Object;

        var resolver = new ModelResolver(
            configOptions,
            providers,
            controlPlaneStore);

        var router = new SmartRouter(
            resolver,
            this._costService,
            this._healthStore,
            this._quotaTracker,
            this._routingScoreCalculator,
            this._smartRouterLogger);

        return await router.GetCandidatesAsync(
            SingleProviderModel,
            streaming: false).ConfigureAwait(false);
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(13)]
    public async Task<IList<EnrichedCandidate>> SmartRouter_GetCandidatesAsync_MultipleProviders(int providerCount)
    {
        var config = this.CreateSynaxisConfiguration(providerCount, 10, 5);
        var configOptions = Options.Create(config);
        var providers = new ProviderRegistry(configOptions);
        var controlPlaneStore = CreateMockControlPlaneStore().Object;

        var resolver = new ModelResolver(
            configOptions,
            providers,
            controlPlaneStore);

        var router = new SmartRouter(
            resolver,
            this._costService,
            this._healthStore,
            this._quotaTracker,
            this._routingScoreCalculator,
            this._smartRouterLogger);

        return await router.GetCandidatesAsync(
            MultipleProviderModel,
            streaming: false).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<IList<EnrichedCandidate>> SmartRouter_GetCandidatesAsync_AliasResolution()
    {
        return await this._smartRouter.GetCandidatesAsync(
            DefaultAlias,
            streaming: false).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<IList<EnrichedCandidate>> SmartRouter_GetCandidatesAsync_WithStreamingCapability()
    {
        return await this._smartRouter.GetCandidatesAsync(
            SingleProviderModel,
            streaming: true).ConfigureAwait(false);
    }

    private SynaxisConfiguration CreateSynaxisConfiguration(int providerCount, int canonicalModelCount, int aliasCount)
    {
        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>(StringComparer.Ordinal),
            CanonicalModels = new List<CanonicalModelConfig>(),
            Aliases = new Dictionary<string, AliasConfig>(StringComparer.Ordinal),
            JwtSecret = "test-jwt-secret",
            JwtIssuer = "test-issuer",
            JwtAudience = "test-audience",
            MasterKey = "test-master-key",
        };

        for (int i = 0; i < providerCount; i++)
        {
            var providerKey = $"provider-{i}";
            config.Providers[providerKey] = new ProviderConfig
            {
                Key = providerKey,
                Type = "openai",
                Tier = i % 3,
                Models = i == 0
                    ? new List<string> { SingleProviderModel, MultipleProviderModel }
                    : new List<string> { MultipleProviderModel },
                Enabled = true,
            };
        }

        for (int i = 0; i < canonicalModelCount; i++)
        {
            config.CanonicalModels.Add(new CanonicalModelConfig
            {
                Id = $"canonical-model-{i}",
                Provider = $"provider-{i % providerCount}",
                ModelPath = i == 0 ? SingleProviderModel : MultipleProviderModel,
                Streaming = true,
                Tools = true,
                Vision = false,
                StructuredOutput = false,
                LogProbs = false,
            });
        }

        for (int i = 0; i < aliasCount; i++)
        {
            config.Aliases[$"alias-{i}"] = new AliasConfig
            {
                Candidates = new List<string>
                {
                    $"canonical-model-{i % canonicalModelCount}",
                    $"canonical-model-{(i + 1) % canonicalModelCount}",
                },
            };
        }

        config.Aliases[DefaultAlias] = new AliasConfig
        {
            Candidates = new List<string> { "canonical-model-0" },
        };

        return config;
    }

    private static Mock<IControlPlaneStore> CreateMockControlPlaneStore()
    {
        var mock = new Mock<IControlPlaneStore>();
        mock.Setup(x => x.GetGlobalModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken ct) => null);
        mock.Setup(x => x.GetAliasAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tenantId, string alias, CancellationToken ct) => null);
        mock.Setup(x => x.GetComboAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tenantId, string name, CancellationToken ct) => null);
        return mock;
    }
}
