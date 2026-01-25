using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Routing;

namespace Synaxis.InferenceGateway.Application.ChatClients;

public class TieredRoutingChatClient : IChatClient
{
    private readonly IModelResolver _resolver;
    private readonly IServiceProvider _services;
    private readonly ILogger<TieredRoutingChatClient> _logger;
    private readonly ChatClientMetadata _metadata;

    public TieredRoutingChatClient(IModelResolver resolver, IServiceProvider services, ILogger<TieredRoutingChatClient> logger)
    {
        _resolver = resolver;
        _services = services;
        _logger = logger;
        _metadata = new ChatClientMetadata("TieredRouter", new Uri("internal://router"));
    }

    public ChatClientMetadata Metadata => _metadata;

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var modelId = options?.ModelId ?? throw new ArgumentException("ModelId is required", nameof(options));
        
        var required = new RequiredCapabilities(); // Could be derived from options in future
        var resolution = _resolver.Resolve(modelId, required);
        
        var tiered = resolution.Candidates.GroupBy(x => x.Tier).OrderBy(g => g.Key);

        var exceptions = new List<Exception>();

        foreach (var tier in tiered)
        {
            var shuffled = tier.OrderBy(_ => Guid.NewGuid()).ToList();
            foreach (var provider in shuffled)
            {
                try
                {
                    var client = _services.GetRequiredKeyedService<IChatClient>(provider.Key);
                    _logger.LogInformation("Routing {Model} to {Provider} (Tier {Tier})", modelId, provider.Key, provider.Tier);
                    
                    // Strip provider prefix if present to ensure provider receives raw model ID
                    var targetModelId = modelId;
                    if (targetModelId.StartsWith(provider.Key + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        targetModelId = targetModelId.Substring(provider.Key.Length + 1);
                    }

                    var providerOptions = new ChatOptions
                    {
                        ModelId = targetModelId,
                        FrequencyPenalty = options?.FrequencyPenalty,
                        MaxOutputTokens = options?.MaxOutputTokens,
                        PresencePenalty = options?.PresencePenalty,
                        Seed = options?.Seed,
                        StopSequences = options?.StopSequences,
                        Temperature = options?.Temperature,
                        TopK = options?.TopK,
                        TopP = options?.TopP,
                        AdditionalProperties = options?.AdditionalProperties,
                        Tools = options?.Tools,
                        ToolMode = options?.ToolMode,
                        ResponseFormat = options?.ResponseFormat
                    };

                    return await client.GetResponseAsync(chatMessages, providerOptions, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {Provider} failed for model {Model}", provider.Key, modelId);
                    exceptions.Add(ex);
                }
            }
        }

        var errorMsg = $"All providers failed for model {modelId}. Errors: {string.Join("; ", exceptions.Select(e => e.Message))}";
        throw new AggregateException(errorMsg, exceptions);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelId = options?.ModelId ?? throw new ArgumentException("ModelId is required", nameof(options));
        
        var required = new RequiredCapabilities { Streaming = true };
        var resolution = _resolver.Resolve(modelId, required);
        
        var tiered = resolution.Candidates.GroupBy(x => x.Tier).OrderBy(g => g.Key);

        var exceptions = new List<Exception>();
        
        foreach (var tier in tiered)
        {
            var shuffled = tier.OrderBy(_ => Guid.NewGuid()).ToList();
            foreach (var provider in shuffled)
            {
                IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
                try
                {
                    var client = _services.GetRequiredKeyedService<IChatClient>(provider.Key);
                    _logger.LogInformation("Routing {Model} to {Provider} (Tier {Tier})", modelId, provider.Key, provider.Tier);
                    
                    // Strip provider prefix if present
                    var targetModelId = modelId;
                    if (targetModelId.StartsWith(provider.Key + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        targetModelId = targetModelId.Substring(provider.Key.Length + 1);
                    }

                    var providerOptions = new ChatOptions
                    {
                        ModelId = targetModelId,
                        FrequencyPenalty = options?.FrequencyPenalty,
                        MaxOutputTokens = options?.MaxOutputTokens,
                        PresencePenalty = options?.PresencePenalty,
                        Seed = options?.Seed,
                        StopSequences = options?.StopSequences,
                        Temperature = options?.Temperature,
                        TopK = options?.TopK,
                        TopP = options?.TopP,
                        AdditionalProperties = options?.AdditionalProperties,
                        Tools = options?.Tools,
                        ToolMode = options?.ToolMode,
                        ResponseFormat = options?.ResponseFormat
                    };
                    
                    var stream = client.GetStreamingResponseAsync(chatMessages, providerOptions, cancellationToken);
                    enumerator = stream.GetAsyncEnumerator(cancellationToken);
                    
                    if (!await enumerator.MoveNextAsync())
                    {
                        await enumerator.DisposeAsync();
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {Provider} failed to start for model {Model}", provider.Key, modelId);
                    exceptions.Add(ex);
                    if (enumerator != null) await enumerator.DisposeAsync();
                    continue;
                }

                // If we reach here, we have successfully started
                try
                {
                    yield return enumerator.Current;
                    while (await enumerator.MoveNextAsync())
                    {
                        yield return enumerator.Current;
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }

                yield break;
            }
        }

        throw new AggregateException($"All providers failed for model {modelId}", exceptions);
    }

    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
