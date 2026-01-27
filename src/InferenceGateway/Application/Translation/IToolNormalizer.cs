using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Synaxis.InferenceGateway.Application.Translation;

public interface IToolNormalizer
{
    CanonicalResponse NormalizeResponse(CanonicalResponse response);
    ChatResponseUpdate NormalizeUpdate(ChatResponseUpdate update);
}

public class OpenAIToolNormalizer : IToolNormalizer
{
    public CanonicalResponse NormalizeResponse(CanonicalResponse response)
    {
        if (response.ToolCalls == null || response.ToolCalls.Count == 0)
        {
            return response;
        }

        var normalizedCalls = new List<FunctionCallContent>();
        foreach (var toolCall in response.ToolCalls)
        {
            var id = !string.IsNullOrWhiteSpace(toolCall.CallId) ? toolCall.CallId : $"call_{Guid.NewGuid().ToString("N").Substring(0, 24)}";
            // FunctionCallContent usually expects IDictionary<string, object?> for arguments
            // If arguments are already parsed, good. If not, we might need to ensure they are valid.
            // Since CanonicalResponse now uses FunctionCallContent, we assume they are already in the correct type.
            // We just ensure ID is present.
            
            // Create new FunctionCallContent with ID if missing
            // Note: FunctionCallContent properties might be read-only, so we create new instance.
            var newCall = new FunctionCallContent(id, toolCall.Name, toolCall.Arguments);
            normalizedCalls.Add(newCall);
        }

        return response with { ToolCalls = normalizedCalls };
    }

    public ChatResponseUpdate NormalizeUpdate(ChatResponseUpdate update)
    {
        // For streaming, we just pass through for now as ID generation needs state tracking
        // which is complex for a stateless normalizer. 
        // In a full implementation, we'd need a stateful wrapper or context.
        return update;
    }
}