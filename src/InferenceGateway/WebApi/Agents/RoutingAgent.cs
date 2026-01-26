using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.WebApi.Helpers;
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

        // 1. Parse Input
        var openAIRequest = await OpenAIRequestParser.ParseAsync(httpContext, cancellationToken);
        if (openAIRequest == null)
        {
             // Fallback if no body (e.g. GET request or empty) -> treat as default chat
             openAIRequest = new OpenAIRequest { Model = "default" };
        }

        // 2. Map to Canonical
        var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

        // 3. Translate Request
        var translatedRequest = translator.TranslateRequest(canonicalRequest);

        // 4. Prepare Options
        var chatOptions = new ChatOptions { ModelId = translatedRequest.Model };

        // 5. Execute
        var response = await chatClient.GetResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken);
        
        // 6. Translate Response
        var message = response.Messages.FirstOrDefault() ?? new ChatMessage(ChatRole.Assistant, "");
        var toolCalls = message.Contents.OfType<FunctionCallContent>().ToList();
        var canonicalResponse = new CanonicalResponse(message.Text, toolCalls);
        var translatedResponse = translator.TranslateResponse(canonicalResponse);

        // 7. Log Routing Context
        LogRoutingContext(httpContext, openAIRequest.Model, response.AdditionalProperties);

        // 8. Build Agent Response
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

        var openAIRequest = await OpenAIRequestParser.ParseAsync(httpContext, cancellationToken) 
                            ?? new OpenAIRequest { Model = "default" };

        var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);
        var translatedRequest = translator.TranslateRequest(canonicalRequest);
        var chatOptions = new ChatOptions { ModelId = translatedRequest.Model };

        // We can't easily capture the resolved model for RoutingContext in streaming before yielding
        // relies on client logging or future enhancement to yield metadata first.

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

    private void LogRoutingContext(HttpContext? httpContext, string? requestedModel, AdditionalPropertiesDictionary? properties)
    {
        if (httpContext != null && properties != null)
        {
             if (properties.TryGetValue("model_id", out var resolvedModel) &&
                 properties.TryGetValue("provider_name", out var provider))
             {
                 httpContext.Items["RoutingContext"] = new RoutingContext(
                     requestedModel ?? "default", 
                     resolvedModel?.ToString() ?? "", 
                     provider?.ToString() ?? "");
             }
        }
    }
}
