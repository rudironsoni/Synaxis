# Provider System

> **TL;DR**: Providers are pluggable integrations with AI services (OpenAI, Anthropic, Google, etc.) that implement standardized interfaces for chat, embeddings, images, and audio.

## 🎯 Provider Architecture

Synaxis abstracts AI providers behind interfaces, enabling:
- **Multi-provider support**: Use multiple AI providers in same application
- **Hot-swapping**: Switch providers without code changes
- **Routing strategies**: Intelligent provider selection based on cost, quality, or availability
- **Failover**: Automatic fallback when providers fail
- **Testing**: Easy mocking for unit tests

```
┌────────────────────────────────────────────────────────┐
│                  Application Layer                      │
│            (Handlers use provider interfaces)           │
└────────────────────────┬───────────────────────────────┘
                         │
┌────────────────────────▼───────────────────────────────┐
│              Provider Interfaces                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐│
│  │IChatProvider │  │IEmbedding    │  │IImageProvider││
│  │              │  │Provider      │  │              ││
│  └──────────────┘  └──────────────┘  └──────────────┘│
└────────────────────────┬───────────────────────────────┘
                         │
┌────────────────────────▼───────────────────────────────┐
│         Provider Implementations (Pluggable)            │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │  OpenAI  │  │Anthropic │  │  Google  │  ...       │
│  └──────────┘  └──────────┘  └──────────┘            │
└─────────────────────────────────────────────────────────┘
```

## 🔌 Core Provider Interfaces

### Base Interface

```csharp
namespace Synaxis.Abstractions.Providers
{
    /// <summary>
    /// Base interface for all provider clients.
    /// </summary>
    public interface IProviderClient
    {
        /// <summary>
        /// Unique provider identifier (e.g., "openai", "anthropic").
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Human-readable provider name.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Checks if the provider is healthy and available.
        /// </summary>
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets supported models for this provider.
        /// </summary>
        Task<IReadOnlyList<string>> GetSupportedModelsAsync(
            CancellationToken cancellationToken = default);
    }
}
```

### Chat Provider

```csharp
namespace Synaxis.Abstractions.Providers
{
    /// <summary>
    /// Provider for chat/completion operations.
    /// </summary>
    public interface IChatProvider : IProviderClient
    {
        /// <summary>
        /// Generates a chat completion (unary request/response).
        /// </summary>
        Task<ChatResponse> ChatAsync(
            IEnumerable<ChatMessage> messages,
            string model,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams chat completion chunks (server-sent events).
        /// </summary>
        IAsyncEnumerable<ChatChunk> StreamChatAsync(
            IEnumerable<ChatMessage> messages,
            string model,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
```

### Embedding Provider

```csharp
namespace Synaxis.Abstractions.Providers
{
    /// <summary>
    /// Provider for text embedding operations.
    /// </summary>
    public interface IEmbeddingProvider : IProviderClient
    {
        /// <summary>
        /// Generates embeddings for input texts.
        /// </summary>
        Task<EmbeddingResponse> EmbedAsync(
            IEnumerable<string> inputs,
            string model,
            EmbeddingOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the dimension of embeddings for a given model.
        /// </summary>
        Task<int> GetEmbeddingDimensionAsync(
            string model,
            CancellationToken cancellationToken = default);
    }
}
```

### Image Provider

```csharp
namespace Synaxis.Abstractions.Providers
{
    /// <summary>
    /// Provider for image generation operations.
    /// </summary>
    public interface IImageProvider : IProviderClient
    {
        /// <summary>
        /// Generates images from text prompt.
        /// </summary>
        Task<ImageResponse> GenerateImageAsync(
            string prompt,
            string model,
            ImageGenerationOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Edits an existing image based on prompt.
        /// </summary>
        Task<ImageResponse> EditImageAsync(
            byte[] image,
            string prompt,
            string model,
            ImageEditOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
```

### Audio Provider

```csharp
namespace Synaxis.Abstractions.Providers
{
    /// <summary>
    /// Provider for audio transcription and synthesis.
    /// </summary>
    public interface IAudioProvider : IProviderClient
    {
        /// <summary>
        /// Transcribes audio to text.
        /// </summary>
        Task<TranscriptionResponse> TranscribeAsync(
            byte[] audio,
            string model,
            TranscriptionOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Synthesizes text to speech.
        /// </summary>
        Task<AudioResponse> SynthesizeAsync(
            string text,
            string model,
            SynthesisOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
```

## 🏗️ Implementing a Provider

### Example: OpenAI Chat Provider

```csharp
namespace Synaxis.Providers.OpenAI
{
    public class OpenAIChatProvider : IChatProvider
    {
        private readonly OpenAIClient _client;
        private readonly ILogger<OpenAIChatProvider> _logger;

        public string ProviderId => "openai";
        public string ProviderName => "OpenAI";

        public OpenAIChatProvider(
            OpenAIClient client,
            ILogger<OpenAIChatProvider> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<bool> IsHealthyAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.GetModelsAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI health check failed");
                return false;
            }
        }

        public async Task<IReadOnlyList<string>> GetSupportedModelsAsync(
            CancellationToken cancellationToken = default)
        {
            var models = await _client.GetModelsAsync(cancellationToken);
            return models.Data
                .Where(m => m.Id.StartsWith("gpt-"))
                .Select(m => m.Id)
                .ToList();
        }

        public async Task<ChatResponse> ChatAsync(
            IEnumerable<ChatMessage> messages,
            string model,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var request = new OpenAIChatRequest
            {
                Model = model,
                Messages = messages.Select(m => new OpenAIChatMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList(),
                Temperature = options?.Temperature,
                MaxTokens = options?.MaxTokens
            };

            var response = await _client.PostAsync<OpenAIChatRequest, OpenAIChatResponse>(
                "/chat/completions",
                request,
                cancellationToken);

            return new ChatResponse
            {
                Id = response.Id,
                Model = response.Model,
                Content = response.Choices[0].Message.Content,
                FinishReason = MapFinishReason(response.Choices[0].FinishReason),
                Usage = new TokenUsage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = response.Usage.CompletionTokens,
                    TotalTokens = response.Usage.TotalTokens
                }
            };
        }

        public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
            IEnumerable<ChatMessage> messages,
            string model,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var request = new OpenAIChatRequest
            {
                Model = model,
                Messages = messages.Select(m => new OpenAIChatMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList(),
                Stream = true,
                Temperature = options?.Temperature,
                MaxTokens = options?.MaxTokens
            };

            await foreach (var chunk in _client.StreamAsync<OpenAIStreamChunk>(
                "/chat/completions",
                request,
                cancellationToken))
            {
                if (chunk.Choices.Count > 0)
                {
                    var delta = chunk.Choices[0].Delta;
                    yield return new ChatChunk
                    {
                        Id = chunk.Id,
                        Delta = delta.Content ?? string.Empty,
                        FinishReason = MapFinishReason(chunk.Choices[0].FinishReason)
                    };
                }
            }
        }

        private static FinishReason MapFinishReason(string? reason)
        {
            return reason switch
            {
                "stop" => FinishReason.Stop,
                "length" => FinishReason.Length,
                "content_filter" => FinishReason.ContentFilter,
                _ => FinishReason.Unknown
            };
        }
    }
}
```

## 📝 Provider Registration

### Manual Registration

```csharp
using Synaxis.Providers.OpenAI.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register OpenAI provider
builder.Services.AddOpenAIProvider(options =>
{
    options.ApiKey = builder.Configuration["OpenAI:ApiKey"];
    options.OrganizationId = builder.Configuration["OpenAI:OrganizationId"];
    options.BaseUrl = "https://api.openai.com/v1";
    options.TimeoutSeconds = 30;
    options.MaxRetries = 3;
});

// Register Anthropic provider
builder.Services.AddAnthropicProvider(options =>
{
    options.ApiKey = builder.Configuration["Anthropic:ApiKey"];
    options.BaseUrl = "https://api.anthropic.com/v1";
});

// Register Google provider
builder.Services.AddGoogleProvider(options =>
{
    options.ApiKey = builder.Configuration["Google:ApiKey"];
    options.ProjectId = builder.Configuration["Google:ProjectId"];
});
```

### Extension Method Pattern

```csharp
namespace Synaxis.Providers.OpenAI.DependencyInjection
{
    public static class OpenAIProviderExtensions
    {
        public static IServiceCollection AddOpenAIProvider(
            this IServiceCollection services,
            Action<OpenAIOptions> configureOptions)
        {
            // Configure options
            services.Configure(configureOptions);

            // Register HTTP client with retry policy
            services.AddHttpClient<OpenAIClient>()
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());

            // Register provider implementations
            services.AddSingleton<IChatProvider, OpenAIChatProvider>();
            services.AddSingleton<IEmbeddingProvider, OpenAIEmbeddingProvider>();
            services.AddSingleton<IImageProvider, OpenAIImageProvider>();
            services.AddSingleton<IAudioProvider, OpenAIAudioProvider>();

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }
    }
}
```

## 🎯 Provider Selection & Routing

### Provider Selector Interface

```csharp
namespace Synaxis.Abstractions.Routing
{
    /// <summary>
    /// Selects the best provider for a given request.
    /// </summary>
    public interface IProviderSelector
    {
        /// <summary>
        /// Selects a provider based on request criteria.
        /// </summary>
        Task<IProviderClient> SelectProviderAsync(
            ProviderSelectionCriteria criteria,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available providers for a capability.
        /// </summary>
        Task<IReadOnlyList<IProviderClient>> GetAvailableProvidersAsync(
            ProviderCapability capability,
            CancellationToken cancellationToken = default);
    }

    public class ProviderSelectionCriteria
    {
        public required ProviderCapability Capability { get; init; }
        public string? ModelId { get; init; }
        public string? PreferredProviderId { get; init; }
        public RoutingStrategy Strategy { get; init; } = RoutingStrategy.CostOptimized;
    }

    public enum ProviderCapability
    {
        Chat,
        Embedding,
        ImageGeneration,
        AudioTranscription,
        AudioSynthesis
    }

    public enum RoutingStrategy
    {
        CostOptimized,
        QualityOptimized,
        LatencyOptimized,
        RoundRobin,
        PreferredProvider
    }
}
```

### Tiered Routing Strategy

See [ADR-002: Tiered Routing Strategy](../adr/002-tiered-routing-strategy.md) for full details.

```csharp
public class TieredProviderSelector : IProviderSelector
{
    private readonly IEnumerable<IChatProvider> _chatProviders;
    private readonly IHealthStore _healthStore;
    private readonly IQuotaTracker _quotaTracker;

    public async Task<IProviderClient> SelectProviderAsync(
        ProviderSelectionCriteria criteria,
        CancellationToken cancellationToken)
    {
        // Tier 1: User-preferred provider
        if (criteria.PreferredProviderId != null)
        {
            var preferred = _chatProviders
                .FirstOrDefault(p => p.ProviderId == criteria.PreferredProviderId);
            
            if (preferred != null && await IsAvailable(preferred))
            {
                return preferred;
            }
        }

        // Tier 2: Free tier providers (ULTRA MISER MODE™)
        var freeTierProviders = await GetHealthyProviders(tier: 2);
        foreach (var provider in freeTierProviders)
        {
            if (await HasQuota(provider))
            {
                return provider;
            }
        }

        // Tier 3: Paid providers
        var paidProviders = await GetHealthyProviders(tier: 3);
        foreach (var provider in paidProviders)
        {
            if (await HasQuota(provider))
            {
                return provider;
            }
        }

        // Tier 4: Emergency (any healthy provider)
        var emergencyProvider = _chatProviders
            .FirstOrDefault(p => await IsAvailable(p));

        if (emergencyProvider == null)
        {
            throw new NoAvailableProviderException(
                $"No available providers for {criteria.Capability}");
        }

        return emergencyProvider;
    }

    private async Task<bool> IsAvailable(IProviderClient provider)
    {
        var isHealthy = await _healthStore.IsHealthyAsync(provider.ProviderId);
        return isHealthy && await provider.IsHealthyAsync();
    }

    private async Task<bool> HasQuota(IProviderClient provider)
    {
        return await _quotaTracker.HasRemainingQuotaAsync(provider.ProviderId);
    }
}
```

### Usage in Handlers

```csharp
public class ChatCompletionHandler 
    : ICommandHandler<ChatCompletionRequest, ChatCompletionResponse>
{
    private readonly IProviderSelector _providerSelector;

    public async Task<ChatCompletionResponse> HandleAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        // Select provider based on request
        var provider = await _providerSelector.SelectProviderAsync(
            new ProviderSelectionCriteria
            {
                Capability = ProviderCapability.Chat,
                ModelId = request.Model,
                PreferredProviderId = request.PreferredProvider,
                Strategy = request.RoutingStrategy ?? RoutingStrategy.CostOptimized
            },
            cancellationToken);

        // Cast to specific provider interface
        if (provider is not IChatProvider chatProvider)
        {
            throw new InvalidOperationException(
                $"Provider {provider.ProviderId} does not support chat");
        }

        // Execute request
        var response = await chatProvider.ChatAsync(
            request.Messages,
            request.Model,
            new ChatOptions
            {
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens
            },
            cancellationToken);

        return MapToContractResponse(response);
    }
}
```

## 🔄 Provider Failover

```csharp
public class ResilientProviderClient : IChatProvider
{
    private readonly IProviderSelector _selector;
    private readonly ILogger<ResilientProviderClient> _logger;

    public async Task<ChatResponse> ChatAsync(
        IEnumerable<ChatMessage> messages,
        string model,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        var maxAttempts = 3;
        var exceptions = new List<Exception>();

        while (attempts < maxAttempts)
        {
            attempts++;

            try
            {
                // Select provider for this attempt
                var provider = await _selector.SelectProviderAsync(
                    new ProviderSelectionCriteria
                    {
                        Capability = ProviderCapability.Chat,
                        ModelId = model
                    },
                    cancellationToken);

                if (provider is IChatProvider chatProvider)
                {
                    // Attempt request
                    return await chatProvider.ChatAsync(
                        messages,
                        model,
                        options,
                        cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Chat request failed (attempt {Attempt}/{MaxAttempts})",
                    attempts,
                    maxAttempts);

                exceptions.Add(ex);

                if (attempts >= maxAttempts)
                {
                    throw new AggregateException(
                        "All provider attempts failed",
                        exceptions);
                }

                // Wait before retry
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts)), cancellationToken);
            }
        }

        throw new InvalidOperationException("Unexpected end of retry loop");
    }
}
```

## 🏥 Health Tracking

```csharp
public interface IHealthStore
{
    Task<bool> IsHealthyAsync(string providerId);
    Task MarkUnhealthyAsync(string providerId, TimeSpan cooldown);
    Task MarkHealthyAsync(string providerId);
}

public class RedisHealthStore : IHealthStore
{
    private readonly IConnectionMultiplexer _redis;
    private const string HealthKeyPrefix = "provider:health:";

    public async Task<bool> IsHealthyAsync(string providerId)
    {
        var db = _redis.GetDatabase();
        var key = HealthKeyPrefix + providerId;
        
        // If key exists, provider is unhealthy
        return !await db.KeyExistsAsync(key);
    }

    public async Task MarkUnhealthyAsync(string providerId, TimeSpan cooldown)
    {
        var db = _redis.GetDatabase();
        var key = HealthKeyPrefix + providerId;
        
        // Set key with TTL (auto-expires after cooldown)
        await db.StringSetAsync(key, "unhealthy", cooldown);
    }

    public async Task MarkHealthyAsync(string providerId)
    {
        var db = _redis.GetDatabase();
        var key = HealthKeyPrefix + providerId;
        
        // Remove key immediately
        await db.KeyDeleteAsync(key);
    }
}
```

## 📊 Quota Tracking

```csharp
public interface IQuotaTracker
{
    Task<bool> HasRemainingQuotaAsync(string providerId);
    Task IncrementUsageAsync(string providerId, int tokens);
    Task<QuotaStatus> GetQuotaStatusAsync(string providerId);
}

public class QuotaStatus
{
    public required int UsedTokens { get; init; }
    public required int LimitTokens { get; init; }
    public required TimeSpan ResetIn { get; init; }
    public bool HasQuota => UsedTokens < LimitTokens;
}

public class RedisQuotaTracker : IQuotaTracker
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IOptions<QuotaOptions> _options;

    public async Task<bool> HasRemainingQuotaAsync(string providerId)
    {
        var status = await GetQuotaStatusAsync(providerId);
        return status.HasQuota;
    }

    public async Task IncrementUsageAsync(string providerId, int tokens)
    {
        var db = _redis.GetDatabase();
        var key = $"quota:{providerId}";
        
        var quota = _options.Value.GetQuota(providerId);
        var resetWindow = TimeSpan.FromHours(1); // Per-hour window

        // Increment with sliding window
        await db.StringIncrementAsync(key, tokens);
        await db.KeyExpireAsync(key, resetWindow);
    }

    public async Task<QuotaStatus> GetQuotaStatusAsync(string providerId)
    {
        var db = _redis.GetDatabase();
        var key = $"quota:{providerId}";
        
        var used = (int)(await db.StringGetAsync(key));
        var ttl = await db.KeyTimeToLiveAsync(key);
        var quota = _options.Value.GetQuota(providerId);

        return new QuotaStatus
        {
            UsedTokens = used,
            LimitTokens = quota.TokensPerHour,
            ResetIn = ttl ?? TimeSpan.Zero
        };
    }
}
```

## 🧪 Testing Providers

### Mock Provider

```csharp
public class MockChatProvider : IChatProvider
{
    public string ProviderId => "mock";
    public string ProviderName => "Mock Provider";

    private readonly Queue<ChatResponse> _responses = new();

    public void EnqueueResponse(ChatResponse response)
    {
        _responses.Enqueue(response);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<string>> GetSupportedModelsAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(
            new[] { "mock-model-1", "mock-model-2" });
    }

    public Task<ChatResponse> ChatAsync(
        IEnumerable<ChatMessage> messages,
        string model,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_responses.Count == 0)
        {
            return Task.FromResult(new ChatResponse
            {
                Id = "mock-" + Guid.NewGuid(),
                Model = model,
                Content = "Mock response",
                FinishReason = FinishReason.Stop,
                Usage = new TokenUsage { PromptTokens = 10, CompletionTokens = 20 }
            });
        }

        return Task.FromResult(_responses.Dequeue());
    }

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        IEnumerable<ChatMessage> messages,
        string model,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var words = "This is a mock streaming response".Split(' ');
        
        foreach (var word in words)
        {
            await Task.Delay(10, cancellationToken); // Simulate latency
            yield return new ChatChunk
            {
                Id = "mock-stream",
                Delta = word + " ",
                FinishReason = null
            };
        }

        yield return new ChatChunk
        {
            Id = "mock-stream",
            Delta = "",
            FinishReason = FinishReason.Stop
        };
    }
}
```

### Usage in Tests

```csharp
[Fact]
public async Task ChatCompletionHandler_ReturnsResponse()
{
    // Arrange
    var mockProvider = new MockChatProvider();
    mockProvider.EnqueueResponse(new ChatResponse
    {
        Id = "test-123",
        Content = "Test response",
        FinishReason = FinishReason.Stop
    });

    var handler = new ChatCompletionHandler(mockProvider);

    var request = new ChatCompletionRequest
    {
        Model = "mock-model",
        Messages = [new ChatMessage("user", "Hello")]
    };

    // Act
    var response = await handler.HandleAsync(request, CancellationToken.None);

    // Assert
    Assert.Equal("test-123", response.Id);
    Assert.Equal("Test response", response.Content);
}
```

## 🚫 Anti-Patterns

❌ **Don't**: Tightly couple handlers to specific provider implementations  
✅ **Do**: Reference provider interfaces only

❌ **Don't**: Hardcode provider selection in handlers  
✅ **Do**: Use `IProviderSelector` for dynamic routing

❌ **Don't**: Ignore provider health and quota  
✅ **Do**: Use health store and quota tracker

❌ **Don't**: Swallow provider exceptions  
✅ **Do**: Let exceptions bubble for failover logic

## 📚 Related Documentation

- [ADR-002: Tiered Routing Strategy](../adr/002-tiered-routing-strategy.md) - Provider selection algorithm
- [ADR-014: Explicit Registration Pattern](../adr/014-explicit-registration-pattern.md) - How to register providers
- [Mediator Pattern](./mediator.md) - How handlers use providers
- [Package Architecture](./packages.md) - Where provider packages fit

---

**Next**: Read [ADR Index](./adr-index.md) for a complete list of architectural decisions.
