using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Configuration;
using Synaxis.InferenceGateway.Application.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Synaxis.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[Config(typeof(BenchmarkConfig))]
public class ConfigurationLoadingBenchmarks
{
    private string _configJson = null!;
    private IConfiguration _configuration = null!;

    [GlobalSetup]
    public void Setup()
    {
        _configJson = GenerateTestConfiguration();
        _configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_configJson)))
            .Build();
    }

    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public SynaxisConfiguration LoadConfiguration_FromJson()
    {
        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_configJson)))
            .Build();

        var synaxisConfig = new SynaxisConfiguration();
        config.GetSection("Synaxis:InferenceGateway").Bind(synaxisConfig);
        return synaxisConfig;
    }

    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public SynaxisConfiguration LoadConfiguration_FromIConfiguration()
    {
        var synaxisConfig = new SynaxisConfiguration();
        _configuration.GetSection("Synaxis:InferenceGateway").Bind(synaxisConfig);
        return synaxisConfig;
    }

    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public Dictionary<string, ProviderConfig> LoadProvidersOnly()
    {
        var providers = new Dictionary<string, ProviderConfig>();
        _configuration.GetSection("Synaxis:InferenceGateway:Providers").Bind(providers);
        return providers;
    }

    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public List<CanonicalModelConfig> LoadCanonicalModelsOnly()
    {
        var models = new List<CanonicalModelConfig>();
        _configuration.GetSection("Synaxis:InferenceGateway:CanonicalModels").Bind(models);
        return models;
    }

    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public Dictionary<string, AliasConfig> LoadAliasesOnly()
    {
        var aliases = new Dictionary<string, AliasConfig>();
        _configuration.GetSection("Synaxis:InferenceGateway:Aliases").Bind(aliases);
        return aliases;
    }

    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public SynaxisConfiguration LoadConfiguration_Large()
    {
        var largeConfig = GenerateLargeTestConfiguration();
        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(largeConfig)))
            .Build();

        var synaxisConfig = new SynaxisConfiguration();
        config.GetSection("Synaxis:InferenceGateway").Bind(synaxisConfig);
        return synaxisConfig;
    }

    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public string SerializeConfiguration()
    {
        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["TestProvider"] = new ProviderConfig
                {
                    Type = "openai",
                    Tier = 0,
                    Enabled = true,
                    Key = "test-key"
                }
            },
            CanonicalModels = new List<CanonicalModelConfig>
            {
                new CanonicalModelConfig
                {
                    Id = "test-model",
                    Provider = "TestProvider",
                    ModelPath = "test-model",
                    Streaming = true,
                    Tools = true,
                    Vision = false,
                    StructuredOutput = false,
                    LogProbs = false
                }
            },
            Aliases = new Dictionary<string, AliasConfig>
            {
                ["default"] = new AliasConfig { Candidates = new List<string> { "test-model" } }
            }
        };

        return JsonSerializer.Serialize(config);
    }

    private string GenerateTestConfiguration()
    {
        var config = new
        {
            Synaxis = new
            {
                InferenceGateway = new
                {
                    Providers = new Dictionary<string, object>
                    {
                        ["Provider1"] = new
                        {
                            Type = "openai",
                            Tier = 0,
                            Enabled = true,
                            Key = "test-key-1"
                        },
                        ["Provider2"] = new
                        {
                            Type = "groq",
                            Tier = 0,
                            Enabled = true,
                            Key = "test-key-2"
                        },
                        ["Provider3"] = new
                        {
                            Type = "cohere",
                            Tier = 1,
                            Enabled = true,
                            Key = "test-key-3"
                        }
                    },
                    CanonicalModels = new[]
                    {
                        new
                        {
                            Id = "model-1",
                            Provider = "Provider1",
                            ModelPath = "gpt-4",
                            Streaming = true,
                            Tools = true,
                            Vision = false,
                            StructuredOutput = false,
                            LogProbs = false
                        },
                        new
                        {
                            Id = "model-2",
                            Provider = "Provider2",
                            ModelPath = "llama-3-70b",
                            Streaming = true,
                            Tools = true,
                            Vision = false,
                            StructuredOutput = false,
                            LogProbs = false
                        }
                    },
                    Aliases = new Dictionary<string, object>
                    {
                        ["default"] = new
                        {
                            Candidates = new[] { "model-1", "model-2" }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(config);
    }

    private string GenerateLargeTestConfiguration()
    {
        var providers = new Dictionary<string, object>();
        var models = new List<object>();

        for (int i = 0; i < 20; i++)
        {
            providers[$"Provider{i}"] = new
            {
                Type = i % 2 == 0 ? "openai" : "groq",
                Tier = i % 3,
                Enabled = true,
                Key = $"test-key-{i}"
            };

            models.Add(new
            {
                Id = $"model-{i}",
                Provider = $"Provider{i}",
                ModelPath = $"model-path-{i}",
                Streaming = true,
                Tools = true,
                Vision = i % 2 == 0,
                StructuredOutput = false,
                LogProbs = false
            });
        }

        var aliases = new Dictionary<string, object>
        {
            ["default"] = new
            {
                Candidates = models.Take(5).Select(m => ((dynamic)m).Id).ToArray()
            }
        };

        var config = new
        {
            Synaxis = new
            {
                InferenceGateway = new
                {
                    Providers = providers,
                    CanonicalModels = models,
                    Aliases = aliases
                }
            }
        };

        return JsonSerializer.Serialize(config);
    }
}
