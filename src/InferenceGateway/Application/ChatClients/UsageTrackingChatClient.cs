using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Synaxis.InferenceGateway.Application.ChatClients;

public class UsageTrackingChatClient : DelegatingChatClient
{
    private readonly ILogger<UsageTrackingChatClient> _logger;

    public UsageTrackingChatClient(IChatClient innerClient, ILogger<UsageTrackingChatClient> logger)
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
        
        if (response.Usage != null)
        {
            var model = response.ModelId ?? options?.ModelId ?? "unknown";
            var inputTokens = response.Usage.InputTokenCount ?? 0;
            var outputTokens = response.Usage.OutputTokenCount ?? 0;
            var cost = CalculateCost(model, inputTokens, outputTokens);

            _logger.LogInformation("Model: {Model}, Input: {Input}, Output: {Output}, Cost: {Cost:C6}", 
                model, inputTokens, outputTokens, cost);
        }

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private decimal CalculateCost(string model, long inputTokens, long outputTokens)
    {
        decimal inputRate = 0;
        decimal outputRate = 0;

        string lowerModel = model.ToLowerInvariant();

        if (lowerModel.Contains("llama") || lowerModel.Contains("mixtral") || lowerModel.Contains("gemma"))
        {
            inputRate = 0.70m / 1_000_000m;
            outputRate = 0.80m / 1_000_000m;
        }
        else if (lowerModel.Contains("gemini"))
        {
            inputRate = 0.075m / 1_000_000m; 
            outputRate = 0.30m / 1_000_000m;
        }

        return (inputTokens * inputRate) + (outputTokens * outputRate);
    }
}
