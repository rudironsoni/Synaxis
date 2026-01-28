using Microsoft.Agents;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.WebApi.Helpers;
using Synaxis.InferenceGateway.WebApi.Middleware;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Agents;

public class RoutingAgentThread : AgentThread { }

public class RoutingAgent : AIAgent
{
    public override string Name => "Synaxis";

    private readonly IChatClient _chatClient;
    private readonly ITranslationPipeline _translator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RoutingAgent> _logger;

    public RoutingAgent(
        IChatClient chatClient,
        ITranslationPipeline translator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RoutingAgent> logger)
    {
        _chatClient = chatClient;
        _translator = translator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor?.HttpContext;

        // 1. Parse Input
        var openReq = await OpenAIRequestParser.ParseAsync(httpContext, cancellationToken)
                      ?? new OpenAIRequest { Model = "default" };

        // 2. Map to Canonical
        var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openReq, messages);

        // 3. Translate Request
        var translatedRequest = _translator.TranslateRequest(canonicalRequest);

        // 4. Prepare Options
        var chatOptions = new ChatOptions { ModelId = translatedRequest.Model };

        // 5. Execute
        var response = await _chatClient.GetResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken);

        // 6. Translate Response
        var message = response.Messages.FirstOrDefault() ?? new ChatMessage(ChatRole.Assistant, "");
        var toolCalls = message.Contents.OfType<FunctionCallContent>().ToList();
        var canonicalResponse = new CanonicalResponse(message.Text, toolCalls);
        var translatedResponse = _translator.TranslateResponse(canonicalResponse);

        // 7. Log Routing Context
        if (httpContext != null && response.AdditionalProperties != null)
        {
            if (response.AdditionalProperties.TryGetValue("model_id", out var resolvedModel) &&
                response.AdditionalProperties.TryGetValue("provider_name", out var provider))
            {
                httpContext.Items["RoutingContext"] = new RoutingContext(
                    openReq.Model ?? "default",
                    resolvedModel?.ToString() ?? string.Empty,
                    provider?.ToString() ?? string.Empty);
            }
        }

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
        var httpContext = _httpContextAccessor?.HttpContext;

        // 1. Parse Input
        var openReq = await OpenAIRequestParser.ParseAsync(httpContext, cancellationToken)
                      ?? new OpenAIRequest { Model = "default" };

        // 2. Map to Canonical
        var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openReq, messages);

        // 3. Translate Request
        var translatedRequest = _translator.TranslateRequest(canonicalRequest);

        // 4. Prepare Options
        var chatOptions = new ChatOptions { ModelId = translatedRequest.Model };

        // 5. Execute Streaming
        var updates = _chatClient.GetStreamingResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken);

        // 6. Translate and Yield Updates
        await foreach (var update in updates.WithCancellation(cancellationToken))
        {
            var translatedUpdate = _translator.TranslateUpdate(update);

            // 7. Log Routing Context (on first update if available)
            if (httpContext != null && translatedUpdate.AdditionalProperties != null && !httpContext.Items.ContainsKey("RoutingContext"))
            {
                if (translatedUpdate.AdditionalProperties.TryGetValue("model_id", out var resolvedModel) &&
                    translatedUpdate.AdditionalProperties.TryGetValue("provider_name", out var provider))
                {
                    httpContext.Items["RoutingContext"] = new RoutingContext(
                        openReq.Model ?? "default",
                        resolvedModel?.ToString() ?? string.Empty,
                        provider?.ToString() ?? string.Empty);
                }
            }

            yield return new AgentResponseUpdate
            {
                Role = translatedUpdate.Role,
                Contents = translatedUpdate.Contents,
                AdditionalProperties = translatedUpdate.AdditionalProperties
            };
        }
    }

    public override ValueTask<AgentThread> GetNewThreadAsync(CancellationToken cancellationToken = default)
        => new ValueTask<AgentThread>(new RoutingAgentThread());

    public override ValueTask<AgentThread> DeserializeThreadAsync(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => new ValueTask<AgentThread>(new RoutingAgentThread());
}
