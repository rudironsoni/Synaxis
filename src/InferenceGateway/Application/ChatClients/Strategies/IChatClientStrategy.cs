using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Application.ChatClients.Strategies;

public interface IChatClientStrategy
{
    // Determines if this strategy handles the given provider type (e.g. "Groq", "OpenAI")
    bool CanHandle(string providerType);

    // Executes non-streaming request
    Task<ChatResponse> ExecuteAsync(
        IChatClient client, 
        IEnumerable<ChatMessage> messages, 
        ChatOptions options, 
        CancellationToken ct);

    // Executes streaming request
    IAsyncEnumerable<ChatResponseUpdate> ExecuteStreamingAsync(
        IChatClient client, 
        IEnumerable<ChatMessage> messages, 
        ChatOptions options, 
        CancellationToken ct);
}
