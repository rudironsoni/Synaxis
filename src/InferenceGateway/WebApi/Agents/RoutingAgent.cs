using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.WebApi.Middleware;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Agents;

public class RoutingAgentThread : AgentThread { }

public class RoutingAgent : AIAgent
{
    public override string Name => "Synaxis";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RoutingAgent> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RoutingAgent(IServiceScopeFactory scopeFactory, ILogger<RoutingAgent> logger, IHttpContextAccessor httpContextAccessor)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var translator = scope.ServiceProvider.GetRequiredService<ITranslationPipeline>();
        var httpContext = _httpContextAccessor.HttpContext;

        var openAIRequest = await Helpers.OpenAIRequestParser.ParseAsync(httpContext, cancellationToken);
        var modelId = !string.IsNullOrWhiteSpace(openAIRequest?.Model) ? openAIRequest.Model : "default";

        var canonicalRequest = new CanonicalRequest(
            EndpointKind.ChatCompletions,
            modelId,
            messages.ToList(),
            Tools: MapTools(openAIRequest?.Tools),
            ToolChoice: openAIRequest?.ToolChoice,
            ResponseFormat: openAIRequest?.ResponseFormat,
            AdditionalOptions: new ChatOptions
            {
                Temperature = (float?)openAIRequest?.Temperature,
                TopP = (float?)openAIRequest?.TopP,
                MaxOutputTokens = openAIRequest?.MaxTokens,
                StopSequences = MapStopSequences(openAIRequest?.Stop)
            });

        var translatedRequest = translator.TranslateRequest(canonicalRequest);

        // Pass the model ID to the client; SmartRoutingChatClient will handle resolution
        var chatOptions = new ChatOptions { ModelId = translatedRequest.Model };

        var response = await chatClient.GetResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken);
        var message = response.Messages.FirstOrDefault() ?? new ChatMessage(ChatRole.Assistant, "");
        var toolCalls = message.Contents.OfType<FunctionCallContent>().ToList();
        var canonicalResponse = new CanonicalResponse(message.Text, toolCalls);
        var translatedResponse = translator.TranslateResponse(canonicalResponse);

        // Attempt to log RoutingContext if metadata is available
        if (httpContext != null && response.AdditionalProperties != null)
        {
             if (response.AdditionalProperties.TryGetValue("model_id", out var resolvedModel) &&
                 response.AdditionalProperties.TryGetValue("provider_name", out var provider))
             {
                 httpContext.Items["RoutingContext"] = new RoutingContext(modelId, resolvedModel?.ToString() ?? "", provider?.ToString() ?? "");
             }
        }

        var agentMessage = new ChatMessage(ChatRole.Assistant, translatedResponse.Content);
        if (translatedResponse.ToolCalls != null)
        {
            foreach (var toolCall in translatedResponse.ToolCalls)
            {
                agentMessage.Contents.Add(toolCall);
            }
        }

        return new AgentResponse(agentMessage);
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var translator = scope.ServiceProvider.GetRequiredService<ITranslationPipeline>();
        var httpContext = _httpContextAccessor.HttpContext;

        var openAIRequest = await Helpers.OpenAIRequestParser.ParseAsync(httpContext, cancellationToken);
        var modelId = !string.IsNullOrWhiteSpace(openAIRequest?.Model) ? openAIRequest.Model : "default";

        var canonicalRequest = new CanonicalRequest(
            EndpointKind.ChatCompletions,
            modelId,
            messages.ToList(),
            Tools: MapTools(openAIRequest?.Tools),
            ToolChoice: openAIRequest?.ToolChoice,
            ResponseFormat: openAIRequest?.ResponseFormat,
            AdditionalOptions: new ChatOptions
            {
                Temperature = (float?)openAIRequest?.Temperature,
                TopP = (float?)openAIRequest?.TopP,
                MaxOutputTokens = openAIRequest?.MaxTokens,
                StopSequences = MapStopSequences(openAIRequest?.Stop)
            });

        var translatedRequest = translator.TranslateRequest(canonicalRequest);

        var chatOptions = new ChatOptions { ModelId = translatedRequest.Model };

        // We can't easily capture the resolved model for RoutingContext in streaming before yielding,
        // unless the client yields a metadata update first.
        // For now, we rely on SmartRoutingChatClient logging.

        await foreach (var update in chatClient.GetStreamingResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken))
        {
            var translatedUpdate = translator.TranslateUpdate(update);
            yield return new AgentResponseUpdate(translatedUpdate);
        }
    }

    public override ValueTask<AgentThread> GetNewThreadAsync(CancellationToken cancellationToken = default)
        => new ValueTask<AgentThread>(new RoutingAgentThread());

    public override ValueTask<AgentThread> DeserializeThreadAsync(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => new ValueTask<AgentThread>(new RoutingAgentThread());





    private IList<AITool>? MapTools(List<OpenAITool>? tools)
    {
        if (tools == null) return null;
        var result = new List<AITool>();
        foreach (var tool in tools)
        {
            if (tool.Type == "function" && tool.Function != null)
            {
                var function = AIFunctionFactory.Create(
                    (string args) => Task.CompletedTask, // Dummy delegate, we just need the metadata
                    tool.Function.Name,
                    tool.Function.Description);
                // Note: AIFunctionFactory is tricky for just metadata.
                // Better to use a custom AITool implementation or just pass the raw object if supported.
                // For now, we'll skip complex mapping as Microsoft.Extensions.AI handles tools differently.
                // We might need to pass the raw tools in AdditionalOptions if the ChatClient supports it.
            }
        }
        return null; // Placeholder
    }

    private IList<string>? MapStopSequences(object? stop)
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