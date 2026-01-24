using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Synaxis.Application.ChatClients;

public class TieredRoutingChatClient : IChatClient
{
    private readonly IProviderRegistry _registry;
    private readonly IServiceProvider _services;
    private readonly ILogger<TieredRoutingChatClient> _logger;
    private readonly ChatClientMetadata _metadata;

    public TieredRoutingChatClient(IProviderRegistry registry, IServiceProvider services, ILogger<TieredRoutingChatClient> logger)
    {
        _registry = registry;
        _services = services;
        _logger = logger;
        _metadata = new ChatClientMetadata("TieredRouter", new Uri("internal://router"));
    }

    public ChatClientMetadata Metadata => _metadata;

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var modelId = options?.ModelId ?? throw new ArgumentException("ModelId is required", nameof(options));
        var candidates = _registry.GetCandidates(modelId);
        var tiered = candidates.GroupBy(x => x.Tier).OrderBy(g => g.Key);

        var exceptions = new List<Exception>();

        foreach (var tier in tiered)
        {
            var shuffled = tier.OrderBy(_ => Guid.NewGuid()).ToList();
            foreach (var provider in shuffled)
            {
                try
                {
                    var client = _services.GetRequiredKeyedService<IChatClient>(provider.ServiceKey);
                    _logger.LogInformation("Routing {Model} to {Provider} (Tier {Tier})", modelId, provider.ServiceKey, provider.Tier);
                    return await client.GetResponseAsync(chatMessages, options, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {Provider} failed for model {Model}", provider.ServiceKey, modelId);
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
        var candidates = _registry.GetCandidates(modelId);
        var tiered = candidates.GroupBy(x => x.Tier).OrderBy(g => g.Key);

        var exceptions = new List<Exception>();
        
        foreach (var tier in tiered)
        {
            var shuffled = tier.OrderBy(_ => Guid.NewGuid()).ToList();
            foreach (var provider in shuffled)
            {
                IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
                try
                {
                    var client = _services.GetRequiredKeyedService<IChatClient>(provider.ServiceKey);
                    _logger.LogInformation("Routing {Model} to {Provider} (Tier {Tier})", modelId, provider.ServiceKey, provider.Tier);
                    
                    var stream = client.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
                    enumerator = stream.GetAsyncEnumerator(cancellationToken);
                    
                    if (!await enumerator.MoveNextAsync())
                    {
                        await enumerator.DisposeAsync();
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {Provider} failed to start for model {Model}", provider.ServiceKey, modelId);
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
