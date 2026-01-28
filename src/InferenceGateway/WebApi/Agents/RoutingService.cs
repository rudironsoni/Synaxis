using Microsoft.Agents.AI;
using AgentsAI = Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.WebApi.Helpers;
using Synaxis.InferenceGateway.WebApi.Middleware;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Agents
{
    // Scoped service that contains the routing logic and relies on constructor injection
    public class RoutingService
    {
        private readonly IChatClient _chatClient;
        private readonly ITranslationPipeline _translator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RoutingService> _logger;

        public RoutingService(IChatClient chatClient, ITranslationPipeline translator, IHttpContextAccessor httpContextAccessor, ILogger<RoutingService> logger)
        {
            _chatClient = chatClient;
            _translator = translator;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AgentsAI.AgentResponse> HandleAsync(OpenAIRequest openAIRequest, IEnumerable<ChatMessage> messages, AgentsAI.AgentThread? thread = null, AgentsAI.AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // 1. Input is already parsed and passed as arguments

            // 2. Map to Canonical
            var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

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

            return new AgentsAI.AgentResponse(agentMessage);
        }

        public async IAsyncEnumerable<AgentsAI.AgentResponseUpdate> HandleStreamingAsync(OpenAIRequest openAIRequest, IEnumerable<ChatMessage> messages, AgentsAI.AgentThread? thread = null, AgentsAI.AgentRunOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);
            var translatedRequest = _translator.TranslateRequest(canonicalRequest);
            var chatOptions = new ChatOptions { ModelId = translatedRequest.Model };

            await foreach (var update in _chatClient.GetStreamingResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken))
            {
                var translatedUpdate = _translator.TranslateUpdate(update);
                yield return new AgentsAI.AgentResponseUpdate(translatedUpdate);
            }
        }

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
}
