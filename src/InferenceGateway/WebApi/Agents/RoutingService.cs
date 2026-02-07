// <copyright file="RoutingService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Agents
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Agents.AI;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.Translation;
    using Synaxis.InferenceGateway.WebApi.Helpers;
    using Synaxis.InferenceGateway.WebApi.Middleware;
    using AgentsAI = Microsoft.Agents.AI;

    /// <summary>
    /// Scoped service that contains the routing logic and relies on constructor injection.
    /// </summary>
    public class RoutingService
    {
        private readonly IChatClient _chatClient;
        private readonly ITranslationPipeline _translator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutingService"/> class.
        /// </summary>
        /// <param name="chatClient">The chat client.</param>
        /// <param name="translator">The translation pipeline.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public RoutingService(IChatClient chatClient, ITranslationPipeline translator, IHttpContextAccessor httpContextAccessor)
        {
            this._chatClient = chatClient;
            this._translator = translator;
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Handles the request asynchronously.
        /// </summary>
        /// <param name="openAIRequest">The OpenAI request.</param>
        /// <param name="messages">The chat messages.</param>
        /// <param name="thread">The agent thread.</param>
        /// <param name="options">The agent run options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The agent response.</returns>
        public async Task<AgentsAI.AgentResponse> HandleAsync(OpenAIRequest openAIRequest, IEnumerable<ChatMessage> messages, AgentsAI.AgentThread? thread = null, AgentsAI.AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        {
            var httpContext = this._httpContextAccessor.HttpContext;

            // 1. Input is already parsed and passed as arguments

            // 2. Map to Canonical
            var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);

            // 3. Translate Request
            var translatedRequest = this._translator.TranslateRequest(canonicalRequest);

            // 4. Prepare Options
            var chatOptions = new ChatOptions { ModelId = translatedRequest.model };

            // 5. Execute
            var response = await this._chatClient.GetResponseAsync(translatedRequest.messages, chatOptions, cancellationToken).ConfigureAwait(false);

            // 6. Translate Response
            var message = response.Messages.FirstOrDefault() ?? new ChatMessage(ChatRole.Assistant, string.Empty);
            var toolCalls = message.Contents.OfType<FunctionCallContent>().ToList();
            var canonicalResponse = new CanonicalResponse(message.Text, toolCalls);
            var translatedResponse = this._translator.TranslateResponse(canonicalResponse);

            // 7. Log Routing Context
            LogRoutingContext(httpContext, openAIRequest.Model, response.AdditionalProperties);

            // 8. Build Agent Response
            var agentMessage = new ChatMessage(ChatRole.Assistant, translatedResponse.content);
            if (translatedResponse.toolCalls != null)
            {
                foreach (var toolCall in translatedResponse.toolCalls)
                {
                    agentMessage.Contents.Add(toolCall);
                }
            }

            return new AgentsAI.AgentResponse(agentMessage);
        }

        /// <summary>
        /// Handles the streaming request asynchronously.
        /// </summary>
        /// <param name="openAIRequest">The OpenAI request.</param>
        /// <param name="messages">The chat messages.</param>
        /// <param name="thread">The agent thread.</param>
        /// <param name="options">The agent run options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The agent response updates.</returns>
        public async IAsyncEnumerable<AgentsAI.AgentResponseUpdate> HandleStreamingAsync(OpenAIRequest openAIRequest, IEnumerable<ChatMessage> messages, AgentsAI.AgentThread? thread = null, AgentsAI.AgentRunOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openAIRequest, messages);
            var translatedRequest = this._translator.TranslateRequest(canonicalRequest);
            var chatOptions = new ChatOptions { ModelId = translatedRequest.model };

            await foreach (var update in this._chatClient.GetStreamingResponseAsync(translatedRequest.messages, chatOptions, cancellationToken).ConfigureAwait(false))
            {
                var translatedUpdate = this._translator.TranslateUpdate(update);
                yield return new AgentsAI.AgentResponseUpdate(translatedUpdate);
            }
        }

        private static void LogRoutingContext(HttpContext? httpContext, string? requestedModel, AdditionalPropertiesDictionary? properties)
        {
            if (httpContext != null &&
                properties != null &&
                properties.TryGetValue("model_id", out var resolvedModel) &&
                properties.TryGetValue("provider_name", out var provider))
            {
                httpContext.Items["RoutingContext"] = new RoutingContext(
                    requestedModel ?? string.Empty,
                    resolvedModel?.ToString() ?? string.Empty,
                    provider?.ToString() ?? string.Empty);
            }
        }
    }
}
