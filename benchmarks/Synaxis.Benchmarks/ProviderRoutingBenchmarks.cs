using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[Config(typeof(BenchmarkConfig))]
public class ProviderRoutingBenchmarks
{
    private List<ProviderConfig> _providers = null!;
    private List<CanonicalModelConfig> _models = null!;
    private List<ModelCost> _costs = null!;
    private CancellationToken _cancellationToken;

    [GlobalSetup]
    public void Setup()
    {
        _cancellationToken = CancellationToken.None;
        _providers = new List<ProviderConfig>();
        _models = new List<CanonicalModelConfig>();
        _costs = new List<ModelCost>();

        for (int i = 0; i < 5; i++)
        {
            _providers.Add(new ProviderConfig
            {
                Type = i % 2 == 0 ? "openai" : "groq",
                Tier = i % 3,
                Enabled = true,
                Key = $"test-key-{i}"
            });

            _models.Add(new CanonicalModelConfig
            {
                Id = $"model-{i}",
                Provider = $"Provider{i}",
                ModelPath = $"model-path-{i}",
                Streaming = true,
                Tools = true,
                Vision = false,
                StructuredOutput = false,
                LogProbs = false
            });

            _costs.Add(new ModelCost
            {
                Provider = $"Provider{i}",
                Model = $"model-path-{i}",
                CostPerToken = 0.001m * (i + 1),
                FreeTier = i == 0
            });
        }
    }

    [Benchmark]
    [BenchmarkCategory("Routing")]
    public List<EnrichedCandidate> CreateEnrichedCandidates_Small()
    {
        var candidates = new List<EnrichedCandidate>();
        for (int i = 0; i < 3; i++)
        {
            candidates.Add(new EnrichedCandidate(
                _providers[i],
                _costs[i],
                _models[i].ModelPath
            ));
        }
        return candidates;
    }

    [Benchmark]
    [BenchmarkCategory("Routing")]
    public List<EnrichedCandidate> CreateEnrichedCandidates_Large()
    {
        var candidates = new List<EnrichedCandidate>();
        for (int i = 0; i < 20; i++)
        {
            var provider = new ProviderConfig
            {
                Type = i % 2 == 0 ? "openai" : "groq",
                Tier = i % 3,
                Enabled = true,
                Key = $"test-key-{i}"
            };

            var cost = new ModelCost
            {
                Provider = $"Provider{i}",
                Model = $"model-path-{i}",
                CostPerToken = 0.001m * (i + 1),
                FreeTier = i == 0
            };

            candidates.Add(new EnrichedCandidate(
                provider,
                cost,
                $"model-path-{i}"
            ));
        }
        return candidates;
    }

    [Benchmark]
    [BenchmarkCategory("Routing")]
    public List<EnrichedCandidate> SortEnrichedCandidates_ByCost()
    {
        var candidates = new List<EnrichedCandidate>();
        for (int i = 0; i < 10; i++)
        {
            candidates.Add(new EnrichedCandidate(
                _providers[i % 5],
                _costs[i % 5],
                _models[i % 5].ModelPath
            ));
        }

        return candidates
            .OrderByDescending(c => c.IsFree)
            .ThenBy(c => c.CostPerToken)
            .ThenBy(c => c.Config.Tier)
            .ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Routing")]
    public List<EnrichedCandidate> FilterEnrichedCandidates_ByTier()
    {
        var candidates = new List<EnrichedCandidate>();
        for (int i = 0; i < 10; i++)
        {
            candidates.Add(new EnrichedCandidate(
                _providers[i % 5],
                _costs[i % 5],
                _models[i % 5].ModelPath
            ));
        }

        return candidates
            .Where(c => c.Config.Tier == 0)
            .ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Routing")]
    public List<EnrichedCandidate> FilterEnrichedCandidates_ByFreeTier()
    {
        var candidates = new List<EnrichedCandidate>();
        for (int i = 0; i < 10; i++)
        {
            candidates.Add(new EnrichedCandidate(
                _providers[i % 5],
                _costs[i % 5],
                _models[i % 5].ModelPath
            ));
        }

        return candidates
            .Where(c => c.IsFree)
            .ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Routing")]
    public ResolutionResult CreateResolutionResult()
    {
        var candidates = _providers.Take(3).ToList();
        var canonicalId = new CanonicalModelId("Provider0", "model-path-0");

        return new ResolutionResult("test-model", canonicalId, candidates);
    }

    [Benchmark]
    [BenchmarkCategory("Routing")]
    public List<EnrichedCandidate> FullRoutingPipeline()
    {
        var candidates = new List<EnrichedCandidate>();
        for (int i = 0; i < 10; i++)
        {
            candidates.Add(new EnrichedCandidate(
                _providers[i % 5],
                _costs[i % 5],
                _models[i % 5].ModelPath
            ));
        }

        return candidates
            .Where(c => c.Config.Enabled)
            .Where(c => c.Config.Tier <= 1)
            .OrderByDescending(c => c.IsFree)
            .ThenBy(c => c.CostPerToken)
            .ThenBy(c => c.Config.Tier)
            .ToList();
    }
}
