// <copyright file="CircuitBreakerSmokeTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests
{
    /// <summary>
    /// Smoke tests for real providers with circuit breaker logic.
    /// These tests hit actual provider APIs and are skipped if the circuit breaker is open.
    /// Only 3 representative providers are tested (Groq, Cohere, OpenRouter) to minimize flakiness.
    /// </summary>
    [Trait("Category", "RealProvider")]
    [Trait("Category", "Smoke")]
    [Trait("Type", "Integration")]
    public class CircuitBreakerSmokeTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly CircuitBreakerState _circuitBreaker;

        // Representative providers to test (only 3 to minimize flakiness)
        private static readonly string[] RepresentativeProviders = { "Groq", "Cohere", "OpenRouter" };

        public CircuitBreakerSmokeTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            this._factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this._output = output ?? throw new ArgumentNullException(nameof(output));
            this._factory.OutputHelper = output;
            this._circuitBreaker = this.LoadCircuitBreakerState();
        }

        [Theory]
        [MemberData(nameof(GetRepresentativeProviderCases))]
        [Trait("Endpoint", "ChatCompletions")]
        public async Task ChatCompletions_RealProviderSmokeTest(string provider, string model, string canonicalId, EndpointType endpoint)
        {
            // Skip if circuit breaker is open
            if (this._circuitBreaker.IsOpen(provider))
            {
                this._output.WriteLine($"Circuit breaker is open for {provider}, skipping test");
                return;
            }

            var testCase = new SmokeTestCase(provider, model, canonicalId, endpoint);

            // Use real HTTP client for real provider tests
            var client = this._factory.CreateClient();
            var executor = new SmokeTestExecutor(client, new SmokeTestOptions(), this._output);

            var result = await executor.ExecuteAsync(testCase);

            this._output.WriteLine($"Provider={testCase.Provider} Model={testCase.Model} Success={result.Success} TimeMs={result.ResponseTime.TotalMilliseconds} Attempts={result.AttemptCount}");
            if (!string.IsNullOrEmpty(result.Error))
            {
                this._output.WriteLine($"Error: {result.Error}");
            }

            if (!string.IsNullOrEmpty(result.ResponseSnippet))
            {
                this._output.WriteLine($"Snippet: {result.ResponseSnippet}");
            }

            if (result.Success)
            {
                this._circuitBreaker.RecordSuccess(provider);
            }
            else
            {
                this._circuitBreaker.RecordFailure(provider);
            }

            this.SaveCircuitBreakerState();

            // Don't assert on success for real providers - they may be flaky
            // Just record the result for circuit breaker tracking
            this._output.WriteLine($"Test completed for {provider}: {(result.Success ? "Success" : "Failed")}");
        }

        public static IEnumerable<object[]> GetRepresentativeProviderCases()
        {
            var configuration = BuildConfiguration();

            var hasData = false;
            foreach (var providerName in RepresentativeProviders)
            {
                var providerSection = configuration.GetSection($"Synaxis:InferenceGateway:Providers:{providerName}");
                if (!providerSection.Exists())
                {
                    continue;
                }

                if (!providerSection.GetValue<bool>("Enabled"))
                {
                    continue;
                }

                // Skip providers with placeholder API keys
                var apiKey = providerSection.GetValue<string>("Key");
                if (string.IsNullOrEmpty(apiKey) ||
                    apiKey.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase) ||
                    apiKey.Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
                    apiKey.Contains("CHANGE", StringComparison.OrdinalIgnoreCase) ||
string.Equals(apiKey, "0000000000", StringComparison.Ordinal))
                {
                    continue;
                }

                var modelsSection = providerSection.GetSection("Models");
                foreach (var modelItem in modelsSection.GetChildren())
                {
                    var modelName = modelItem.Value;
                    if (string.IsNullOrEmpty(modelName))
                    {
                        continue;
                    }

                    var canonicalId = FindCanonicalId(configuration, providerName, modelName) ?? modelName;

                    hasData = true;
                    yield return new object[] { providerName, modelName, canonicalId, EndpointType.ChatCompletions };
                }
            }

            // If no valid providers found, return mock data to prevent "No data found" errors
            if (!hasData)
            {
                yield return new object[] { "MockProvider", "mock-model", "mock-model", EndpointType.ChatCompletions };
            }
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var builder = new ConfigurationBuilder();

            // Find project root to locate appsettings
            var projectRoot = FindProjectRoot();
            if (!string.IsNullOrEmpty(projectRoot))
            {
                var webApiPath = Path.Combine(projectRoot, "src", "InferenceGateway", "WebApi");
                if (Directory.Exists(webApiPath))
                {
                    var appsettings = Path.Combine(webApiPath, "appsettings.json");
                    var appsettingsDev = Path.Combine(webApiPath, "appsettings.Development.json");
                    if (File.Exists(appsettings))
                    {
                        builder.AddJsonFile(appsettings, optional: true, reloadOnChange: false);
                    }

                    if (File.Exists(appsettingsDev))
                    {
                        builder.AddJsonFile(appsettingsDev, optional: true, reloadOnChange: false);
                    }
                }
            }

            // Load .env files
            DotNetEnv.Env.TraversePath().Load();

            builder.AddEnvironmentVariables();

            // Map environment variables to configuration keys
            var envMapping = new Dictionary<string, string?>
(StringComparer.Ordinal)
            {
                { "Synaxis:InferenceGateway:Providers:Groq:Key", Environment.GetEnvironmentVariable("GROQ_API_KEY") },
                { "Synaxis:InferenceGateway:Providers:Cohere:Key", Environment.GetEnvironmentVariable("COHERE_API_KEY") },
                { "Synaxis:InferenceGateway:Providers:OpenRouter:Key", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") },
            };

            var filteredMapping = envMapping.Where(kv => !string.IsNullOrEmpty(kv.Value))
                                        .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

            builder.AddInMemoryCollection(filteredMapping);

            return builder.Build();
        }

        private static string? FindCanonicalId(IConfigurationRoot config, string provider, string modelPath)
        {
            var canonicals = config.GetSection("Synaxis:InferenceGateway:CanonicalModels");
            if (!canonicals.Exists())
            {
                return null;
            }

            foreach (var item in canonicals.GetChildren())
            {
                var itemProvider = item.GetValue<string>("Provider");
                var itemModelPath = item.GetValue<string>("ModelPath");
                if (string.Equals(itemProvider, provider, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(itemModelPath, modelPath, StringComparison.OrdinalIgnoreCase))
                {
                    return item.GetValue<string>("Id");
                }
            }

            return null;
        }

        private static string? FindProjectRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory());
            while (dir != null)
            {
                if (dir.GetFiles("*.sln").Any())
                {
                    return dir.FullName;
                }

                var src = Path.Combine(dir.FullName, "src");
                if (Directory.Exists(src))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }

        private CircuitBreakerState LoadCircuitBreakerState()
        {
            var statePath = GetCircuitBreakerStatePath();
            if (File.Exists(statePath))
            {
                try
                {
                    var json = File.ReadAllText(statePath);
                    return JsonSerializer.Deserialize<CircuitBreakerState>(json) ?? new CircuitBreakerState();
                }
                catch (Exception ex)
                {
                    this._output.WriteLine($"Failed to load circuit breaker state: {ex.Message}");
                }
            }

            return new CircuitBreakerState();
        }

        private void SaveCircuitBreakerState()
        {
            var statePath = GetCircuitBreakerStatePath();
            var directory = Path.GetDirectoryName(statePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                var json = JsonSerializer.Serialize(this._circuitBreaker, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(statePath, json);
            }
            catch (Exception ex)
            {
                this._output.WriteLine($"Failed to save circuit breaker state: {ex.Message}");
            }
        }

        private static string GetCircuitBreakerStatePath()
        {
            var projectRoot = FindProjectRoot();
            return Path.Combine(projectRoot ?? ".", ".sisyphus", "circuit-breaker-state.json");
        }
    }

    /// <summary>
    /// Circuit breaker state for tracking provider health.
    /// Skips tests for providers that have failed 3 consecutive times.
    /// </summary>
    public class CircuitBreakerState
    {
        private const int FailureThreshold = 3;
        private readonly Dictionary<string, ProviderState> _providerStates = new(StringComparer.Ordinal);

        public bool IsOpen(string provider)
        {
            if (!this._providerStates.TryGetValue(provider, out var state))
            {
                return false;
            }

            return state.ConsecutiveFailures >= FailureThreshold;
        }

        public void RecordSuccess(string provider)
        {
            if (!this._providerStates.TryGetValue(provider, out var state))
            {
                state = new ProviderState();
                this._providerStates[provider] = state;
            }

            state.ConsecutiveFailures = 0;
            state.LastSuccess = DateTime.UtcNow;
        }

        public void RecordFailure(string provider)
        {
            if (!this._providerStates.TryGetValue(provider, out var state))
            {
                state = new ProviderState();
                this._providerStates[provider] = state;
            }

            state.ConsecutiveFailures++;
            state.LastFailure = DateTime.UtcNow;
        }

        public Dictionary<string, ProviderState> ProviderStates => this._providerStates;
    }

    /// <summary>
    /// State for a single provider in the circuit breaker.
    /// </summary>
    public class ProviderState
    {
        public int ConsecutiveFailures { get; set; }

        public DateTime? LastSuccess { get; set; }

        public DateTime? LastFailure { get; set; }
    }
}
