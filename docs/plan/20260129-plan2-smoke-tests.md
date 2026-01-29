# Plan: Provider/Model Smoke Test System

## Goal
Create an automated smoke test system that validates all permutations of providers, models, and endpoints through the Synaxis gateway using xUnit.

## Design Principles
- **Data-driven**: Test cases generated dynamically from `appsettings.json`
- **End-to-end**: All tests route through the Synaxis gateway (not direct provider calls)
- **xUnit native**: Uses `[Theory]`, `[MemberData]`, and `[Trait]` for filtering
- **Maintainable**: Adding a new provider/model to config automatically creates tests

---

## Architecture

```
tests/InferenceGateway/IntegrationTests/
  SmokeTests/
    Models/
      SmokeTestCase.cs           # Test case record
      SmokeTestResult.cs         # Result record
      EndpointType.cs            # Enum: ChatCompletions, LegacyCompletions
      SmokeTestOptions.cs        # Timeout, retry settings per provider
    Infrastructure/
      SmokeTestDataGenerator.cs  # Generates test cases from config
      SmokeTestExecutor.cs       # Executes tests with retry logic
      RetryPolicy.cs             # Exponential backoff implementation
    ProviderModelSmokeTests.cs   # Main xUnit test class
```

---

## Components

### 1. Models

#### `EndpointType.cs`
```csharp
public enum EndpointType
{
    ChatCompletions,    // /openai/v1/chat/completions
    LegacyCompletions   // /openai/v1/completions
}
```

#### `SmokeTestCase.cs`
```csharp
public record SmokeTestCase(
    string Provider,
    string Model,
    string CanonicalId,
    EndpointType Endpoint,
    int TimeoutMs = 30000,
    int MaxRetries = 3
);
```

#### `SmokeTestResult.cs`
```csharp
public record SmokeTestResult(
    SmokeTestCase Case,
    bool Success,
    TimeSpan ResponseTime,
    string? Error = null,
    string? ResponseSnippet = null,
    int AttemptCount = 1
);
```

#### `SmokeTestOptions.cs`
```csharp
public class SmokeTestOptions
{
    public int DefaultTimeoutMs { get; set; } = 30000;
    public int MaxRetries { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 1000;
    public double RetryBackoffMultiplier { get; set; } = 2.0;
    
    // Per-provider overrides
    public Dictionary<string, ProviderSmokeTestOptions> ProviderOverrides { get; set; } = new();
}

public class ProviderSmokeTestOptions
{
    public int? TimeoutMs { get; set; }
    public int? MaxRetries { get; set; }
}
```

---

### 2. Infrastructure

#### `SmokeTestDataGenerator.cs`
Reads configuration at test discovery time and generates test cases.

```csharp
public static class SmokeTestDataGenerator
{
    private static readonly Lazy<SynaxisConfiguration> _config = new(() => LoadConfiguration());
    
    public static IEnumerable<object[]> GenerateChatCompletionCases()
        => GenerateTestCases(EndpointType.ChatCompletions);
    
    public static IEnumerable<object[]> GenerateLegacyCompletionCases()
        => GenerateTestCases(EndpointType.LegacyCompletions);
    
    private static IEnumerable<object[]> GenerateTestCases(EndpointType endpoint)
    {
        var config = _config.Value;
        
        foreach (var (providerName, provider) in config.Providers)
        {
            if (!provider.Enabled) continue;
            
            foreach (var model in provider.Models)
            {
                var canonicalId = FindCanonicalId(config, providerName, model) ?? model;
                yield return new object[] { providerName, model, canonicalId, endpoint };
            }
        }
    }
    
    private static SynaxisConfiguration LoadConfiguration()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(FindProjectRoot())
            .AddJsonFile("src/InferenceGateway/WebApi/appsettings.json", optional: true)
            .AddJsonFile("src/InferenceGateway/WebApi/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        var synaxisConfig = new SynaxisConfiguration();
        config.GetSection("Synaxis:InferenceGateway").Bind(synaxisConfig);
        return synaxisConfig;
    }
    
    private static string FindProjectRoot()
    {
        // ... (implementation to find root)
    }

    private static string? FindCanonicalId(SynaxisConfiguration config, string provider, string model)
    {
        return config.CanonicalModels
            .FirstOrDefault(c => c.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) 
                              && c.ModelPath == model)?.Id;
    }
}
```

#### `RetryPolicy.cs`
Implements exponential backoff with jitter.

```csharp
public class RetryPolicy
{
    // ... (implementation)
}
```

#### `SmokeTestExecutor.cs`
Executes smoke tests with retry and timeout handling.

```csharp
public class SmokeTestExecutor
{
    // ... (implementation)
}
```

---

### 3. Main Test Class

#### `ProviderModelSmokeTests.cs`

```csharp
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests;

[Trait("Category", "Smoke")]
[Trait("Type", "Integration")]
public class ProviderModelSmokeTests : IClassFixture<SynaxisWebApplicationFactory>
{
    // ... (implementation)
}
```

---

## Usage

### Run All Smoke Tests
```bash
dotnet test --filter "Category=Smoke"
```

### Run Specific Provider
```bash
dotnet test --filter "Category=Smoke&DisplayName~Groq"
```

### Run Specific Endpoint Type
```bash
dotnet test --filter "Category=Smoke&Endpoint=ChatCompletions"
```

### Run with Detailed Output
```bash
dotnet test --filter "Category=Smoke" --logger "console;verbosity=detailed"
```

---

## Configuration Updates Required

### Update `SynaxisWebApplicationFactory.cs`
Add environment variable pass-through for all providers: KiloCode, NVIDIA, Gemini, OpenRouter, HuggingFace.

---

## Success Criteria

1. All tests pass when valid API keys are configured
2. Tests **FAIL** with clear message when API key is missing/placeholder
3. Tests retry on transient failures (rate limits, network issues) with exponential backoff
4. Tests respect configurable timeouts (30s default, per-provider override)
5. Test output shows timing and attempt count
6. CI/CD integration works via environment variables
7. xUnit Traits enable flexible filtering by Category, Endpoint, Provider
