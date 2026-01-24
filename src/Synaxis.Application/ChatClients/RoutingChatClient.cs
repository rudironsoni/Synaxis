using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Synaxis.Application.ChatClients;

public class RoutingChatClient : IChatClient
{
    private readonly IChatClient _groq;
    private readonly IChatClient _gemini;

    public RoutingChatClient(
        [FromKeyedServices("groq")] IChatClient groq,
        [FromKeyedServices("gemini")] IChatClient gemini)
    {
        _groq = groq;
        _gemini = gemini;
    }

    public ChatClientMetadata Metadata => new ChatClientMetadata("RoutingChatClient");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient(options?.ModelId);
        return await client.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = GetClient(options?.ModelId);
        await foreach (var update in client.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private IChatClient GetClient(string? modelId)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            return _gemini; // Default
        }

        if (modelId.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
        {
            return _gemini;
        }

        if (modelId.StartsWith("llama", StringComparison.OrdinalIgnoreCase) ||
            modelId.StartsWith("mixtral", StringComparison.OrdinalIgnoreCase) ||
            modelId.StartsWith("gemma", StringComparison.OrdinalIgnoreCase))
        {
            return _groq;
        }

        return _gemini; // Fallback
    }

    public void Dispose()
    {
        _groq.Dispose();
        _gemini.Dispose();
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
        {
            return Metadata;
        }

        return null;
    }
}
