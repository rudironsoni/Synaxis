using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.Application.Routing;
using System.Text.Json;
using System.Collections.Generic;

namespace Synaxis.InferenceGateway.WebApi.Helpers;

public static class OpenAIRequestMapper
{
    public static CanonicalRequest ToCanonicalRequest(OpenAIRequest openAIRequest, IEnumerable<ChatMessage> messages)
    {
        var modelId = !string.IsNullOrWhiteSpace(openAIRequest.Model) ? openAIRequest.Model : "default";

        return new CanonicalRequest(
            EndpointKind.ChatCompletions,
            modelId,
            messages.ToList(),
            Tools: MapTools(openAIRequest.Tools),
            ToolChoice: openAIRequest.ToolChoice,
            ResponseFormat: openAIRequest.ResponseFormat,
            AdditionalOptions: new ChatOptions
            {
                Temperature = (float?)openAIRequest.Temperature,
                TopP = (float?)openAIRequest.TopP,
                MaxOutputTokens = openAIRequest.MaxTokens,
                StopSequences = MapStopSequences(openAIRequest.Stop)
            });
    }

    private static IList<AITool>? MapTools(List<OpenAITool>? tools)
    {
        if (tools == null) return null;
        var result = new List<AITool>();
        foreach (var tool in tools)
        {
            if (tool.Type == "function" && tool.Function != null)
            {
                // We create a function definition using the AIFunctionFactory for metadata purposes.
                // The actual execution delegate is a dummy since we are just routing.
                var function = AIFunctionFactory.Create(
                    (string args) => Task.CompletedTask, 
                    tool.Function.Name,
                    tool.Function.Description);
                
                result.Add(function);
            }
        }
        return result.Count > 0 ? result : null;
    }

    private static IList<string>? MapStopSequences(object? stop)
    {
        if (stop is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return new List<string> { element.GetString()! };
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        list.Add(item.GetString()!);
                    }
                }
                return list;
            }
        }
        return null;
    }
}
