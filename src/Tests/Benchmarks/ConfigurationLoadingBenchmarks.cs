// <copyright file="ConfigurationLoadingBenchmarks.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.Extensions.Configuration;
using Synaxis.InferenceGateway.Application.Configuration;

/// <summary>
/// Benchmarks for configuration loading performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ConfigurationLoadingBenchmarks
{
    private IConfiguration _smallConfig = null!;
    private IConfiguration _mediumConfig = null!;
    private IConfiguration _largeConfig = null!;
    private IConfiguration _configWithEnvVars = null!;

    /// <summary>
    /// Sets up the benchmark data.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        this._smallConfig = CreateConfiguration(1, 1, 1);
        this._mediumConfig = CreateConfiguration(5, 5, 5);
        this._largeConfig = CreateConfiguration(13, 10, 10);
        this._configWithEnvVars = CreateConfigurationWithEnvironmentVariables(13, 10, 10);
    }

    /// <summary>
    /// Benchmarks binding small configuration.
    /// </summary>
    /// <returns>The bound configuration.</returns>
    [Benchmark]
    public SynaxisConfiguration Bind_SmallConfiguration()
    {
        var config = new SynaxisConfiguration();
        this._smallConfig.GetSection("Synaxis:InferenceGateway").Bind(config);
        return config;
    }

    /// <summary>
    /// Benchmarks binding medium configuration.
    /// </summary>
    /// <returns>The bound configuration.</returns>
    [Benchmark]
    public SynaxisConfiguration Bind_MediumConfiguration()
    {
        var config = new SynaxisConfiguration();
        this._mediumConfig.GetSection("Synaxis:InferenceGateway").Bind(config);
        return config;
    }

    /// <summary>
    /// Benchmarks binding large configuration.
    /// </summary>
    /// <returns>The bound configuration.</returns>
    [Benchmark]
    public SynaxisConfiguration Bind_LargeConfiguration()
    {
        var config = new SynaxisConfiguration();
        this._largeConfig.GetSection("Synaxis:InferenceGateway").Bind(config);
        return config;
    }

    /// <summary>
    /// Benchmarks binding configuration with environment variables.
    /// </summary>
    /// <returns>The bound configuration.</returns>
    [Benchmark]
    public SynaxisConfiguration Bind_ConfigurationWithEnvironmentVariables()
    {
        var config = new SynaxisConfiguration();
        this._configWithEnvVars.GetSection("Synaxis:InferenceGateway").Bind(config);
        return config;
    }

    /// <summary>
    /// Benchmarks getting provider key from small configuration.
    /// </summary>
    /// <returns>The provider key.</returns>
    [Benchmark]
    public string GetProviderKey_SmallConfiguration()
    {
        return this._smallConfig["Synaxis:InferenceGateway:Providers:groq:Key"] ?? string.Empty;
    }

    /// <summary>
    /// Benchmarks getting provider key from large configuration.
    /// </summary>
    /// <returns>The provider key.</returns>
    [Benchmark]
    public string GetProviderKey_LargeConfiguration()
    {
        return this._largeConfig["Synaxis:InferenceGateway:Providers:provider-12:Key"] ?? string.Empty;
    }

    /// <summary>
    /// Benchmarks getting JWT secret from configuration.
    /// </summary>
    /// <returns>The JWT secret.</returns>
    [Benchmark]
    public string GetJwtSecret()
    {
        return this._largeConfig["Synaxis:InferenceGateway:JwtSecret"] ?? string.Empty;
    }

    /// <summary>
    /// Benchmarks getting all provider keys from configuration.
    /// </summary>
    /// <returns>The provider keys.</returns>
    [Benchmark]
    public string[] GetAllProviderKeys()
    {
        var providers = this._largeConfig.GetSection("Synaxis:InferenceGateway:Providers").GetChildren();
        var keys = new string[providers.Count()];
        int i = 0;
        foreach (var provider in providers)
        {
            keys[i++] = provider["Key"] ?? string.Empty;
        }

        return keys;
    }

    private static IConfiguration CreateConfiguration(int providerCount, int canonicalModelCount, int aliasCount)
    {
        var builder = new ConfigurationBuilder();

        var configData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Synaxis:InferenceGateway:JwtSecret"] = "test-jwt-secret",
            ["Synaxis:InferenceGateway:JwtIssuer"] = "test-issuer",
            ["Synaxis:InferenceGateway:JwtAudience"] = "test-audience",
            ["Synaxis:InferenceGateway:MasterKey"] = "test-master-key",
        };

        for (int i = 0; i < providerCount; i++)
        {
            var providerKey = $"provider-{i}";
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Enabled"] = "true";
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Key"] = $"api-key-{i}";
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Type"] = "openai";
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Tier"] = (i % 3).ToString(System.Globalization.CultureInfo.InvariantCulture);
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Models:0"] = $"model-{i}";
        }

        for (int i = 0; i < canonicalModelCount; i++)
        {
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Id"] = $"canonical-model-{i}";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Provider"] = $"provider-{i % providerCount}";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:ModelPath"] = $"model-{i}";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Streaming"] = "true";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Tools"] = "true";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Vision"] = "false";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:StructuredOutput"] = "false";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:LogProbs"] = "false";
        }

        for (int i = 0; i < aliasCount; i++)
        {
            configData[$"Synaxis:InferenceGateway:Aliases:alias-{i}:Candidates:0"] = $"canonical-model-{i % canonicalModelCount}";
            configData[$"Synaxis:InferenceGateway:Aliases:alias-{i}:Candidates:1"] = $"canonical-model-{(i + 1) % canonicalModelCount}";
        }

        builder.AddInMemoryCollection(configData);
        return builder.Build();
    }

    private static IConfiguration CreateConfigurationWithEnvironmentVariables(int providerCount, int canonicalModelCount, int aliasCount)
    {
        var builder = new ConfigurationBuilder();

        var configData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Synaxis:InferenceGateway:JwtSecret"] = "test-jwt-secret",
            ["Synaxis:InferenceGateway:JwtIssuer"] = "test-issuer",
            ["Synaxis:InferenceGateway:JwtAudience"] = "test-audience",
            ["Synaxis:InferenceGateway:MasterKey"] = "test-master-key",
        };

        for (int i = 0; i < providerCount; i++)
        {
            var providerKey = $"provider-{i}";
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Enabled"] = "true";
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Key"] = $"api-key-{i}";
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Type"] = "openai";
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Tier"] = (i % 3).ToString(System.Globalization.CultureInfo.InvariantCulture);
            configData[$"Synaxis:InferenceGateway:Providers:{providerKey}:Models:0"] = $"model-{i}";
        }

        for (int i = 0; i < canonicalModelCount; i++)
        {
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Id"] = $"canonical-model-{i}";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Provider"] = $"provider-{i % providerCount}";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:ModelPath"] = $"model-{i}";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Streaming"] = "true";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Tools"] = "true";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:Vision"] = "false";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:StructuredOutput"] = "false";
            configData[$"Synaxis:InferenceGateway:CanonicalModels:{i}:LogProbs"] = "false";
        }

        for (int i = 0; i < aliasCount; i++)
        {
            configData[$"Synaxis:InferenceGateway:Aliases:alias-{i}:Candidates:0"] = $"canonical-model-{i % canonicalModelCount}";
            configData[$"Synaxis:InferenceGateway:Aliases:alias-{i}:Candidates:1"] = $"canonical-model-{(i + 1) % canonicalModelCount}";
        }

        builder.AddInMemoryCollection(configData);

        var envVars = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["SYNAXIS__INFERENCEGATEWAY__PROVIDERS__PROVIDER-0__KEY"] = "env-api-key-0",
            ["SYNAXIS__INFERENCEGATEWAY__PROVIDERS__PROVIDER-1__KEY"] = "env-api-key-1",
            ["SYNAXIS__INFERENCEGATEWAY__JWTSECRET"] = "env-jwt-secret",
        };

        builder.AddInMemoryCollection(envVars);

        return builder.Build();
    }
}
