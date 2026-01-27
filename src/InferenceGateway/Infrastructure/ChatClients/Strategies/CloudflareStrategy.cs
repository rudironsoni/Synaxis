using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.ChatClients.Strategies;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Infrastructure.ChatClients.Strategies;

public class CloudflareStrategy : IChatClientStrategy
{
    public bool CanHandle(string providerType) => providerType == "Cloudflare";

    public async Task<ChatResponse> ExecuteAsync(
        IChatClient client, 
        IEnumerable<ChatMessage> messages, 
        ChatOptions options, 
        CancellationToken ct)
    {
        // Placeholder for Cloudflare-specific logic (e.g. max_tokens adjustments)
        return await client.GetResponseAsync(messages, options, ct);
    }

    public IAsyncEnumerable<ChatResponseUpdate> ExecuteStreamingAsync(
        IChatClient client, 
        IEnumerable<ChatMessage> messages, 
        ChatOptions options, 
        CancellationToken ct)
    {
        return client.GetStreamingResponseAsync(messages, options, ct);
    }
}
