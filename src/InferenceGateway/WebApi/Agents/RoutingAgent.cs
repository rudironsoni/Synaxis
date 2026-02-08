// <copyright file="RoutingAgent.cs" company="Synaxis">
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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.Routing;
    using Synaxis.InferenceGateway.Application.Translation;
    using Synaxis.InferenceGateway.WebApi.Helpers;
    using Synaxis.InferenceGateway.WebApi.Middleware;

    /// <summary>
    /// Routing agent session.
    /// </summary>
    public class RoutingAgentSession : AgentSession
    {
    }

    /// <summary>
    /// Routing agent that handles inference requests through translation pipeline.
    /// </summary>
    public class RoutingAgent : Microsoft.Agents.AI.AIAgent
    {
        /// <inheritdoc/>
        public override string Name => "Synaxis";

        private readonly IChatClient _chatClient;
        private readonly ITranslationPipeline _translator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutingAgent"/> class.
        /// </summary>
        /// <param name="chatClient">The chat client.</param>
        /// <param name="translator">The translation pipeline.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public RoutingAgent(
            IChatClient chatClient,
            ITranslationPipeline translator,
            IHttpContextAccessor httpContextAccessor)
        {
            this._chatClient = chatClient;
            this._translator = translator;
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Runs the agent asynchronously.
        /// </summary>
        /// <param name="messages">The chat messages.</param>
        /// <param name="session">The agent session.</param>
        /// <param name="options">The agent run options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The agent response.</returns>
        protected override async Task<Microsoft.Agents.AI.AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            Microsoft.Agents.AI.AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var httpContext = this._httpContextAccessor?.HttpContext;

            // 1. Parse Input (or use pre-parsed request from context)
            var openReq = httpContext?.Items["ParsedOpenAIRequest"] as OpenAIRequest
                          ?? await OpenAIRequestParser.ParseAsync(httpContext, cancellationToken).ConfigureAwait(false)
                          ?? new OpenAIRequest { Model = "default" };

            // 2. Map to Canonical
            var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openReq, messages);

            // 3. Translate Request
            var translatedRequest = this._translator.TranslateRequest(canonicalRequest);

            // 4. Prepare Options
            var chatOptions = new ChatOptions { ModelId = translatedRequest.Model };

            // 5. Execute
            var response = await this._chatClient.GetResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken).ConfigureAwait(false);

            // 6. Translate Response
            var message = response.Messages.FirstOrDefault() ?? new ChatMessage(ChatRole.Assistant, string.Empty);
            var toolCalls = message.Contents.OfType<FunctionCallContent>().ToList();
            var canonicalResponse = new CanonicalResponse(message.Text, toolCalls);
            var translatedResponse = this._translator.TranslateResponse(canonicalResponse);

            // 7. Log Routing Context
            if (httpContext != null && response.AdditionalProperties != null &&
                response.AdditionalProperties.TryGetValue("model_id", out var resolvedModel) &&
                response.AdditionalProperties.TryGetValue("provider_name", out var provider))
            {
                httpContext.Items["RoutingContext"] = new RoutingContext(
                    openReq.Model ?? "default",
                    resolvedModel?.ToString() ?? string.Empty,
                    provider?.ToString() ?? string.Empty);
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

            return new Microsoft.Agents.AI.AgentResponse(agentMessage);
        }

        /// <summary>
        /// Runs the agent asynchronously with streaming.
        /// </summary>
        /// <param name="messages">The chat messages.</param>
        /// <param name="session">The agent session.</param>
        /// <param name="options">The agent run options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The agent response updates.</returns>
        protected override async IAsyncEnumerable<Microsoft.Agents.AI.AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            Microsoft.Agents.AI.AgentSession? session = null,
            Microsoft.Agents.AI.AgentRunOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var httpContext = this._httpContextAccessor?.HttpContext;

            // 1. Parse Input (or use pre-parsed request from context)
            var openReq = httpContext?.Items["ParsedOpenAIRequest"] as OpenAIRequest
                          ?? await OpenAIRequestParser.ParseAsync(httpContext, cancellationToken).ConfigureAwait(false)
                          ?? new OpenAIRequest { Model = "default" };

            // 2. Map to Canonical
            var canonicalRequest = OpenAIRequestMapper.ToCanonicalRequest(openReq, messages);

            // 3. Translate Request
            var translatedRequest = this._translator.TranslateRequest(canonicalRequest);

            // 4. Prepare Options
            var chatOptions = new ChatOptions { ModelId = translatedRequest.Model };

            // 5. Execute Streaming
            var updates = this._chatClient.GetStreamingResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken);

            // 6. Translate and Yield Updates
            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var translatedUpdate = this._translator.TranslateUpdate(update);

                // 7. Log Routing Context (on first update if available)
                if (httpContext != null && translatedUpdate.AdditionalProperties != null && !httpContext.Items.ContainsKey("RoutingContext") &&
                    translatedUpdate.AdditionalProperties.TryGetValue("model_id", out var resolvedModel) &&
                    translatedUpdate.AdditionalProperties.TryGetValue("provider_name", out var provider))
                {
                    httpContext.Items["RoutingContext"] = new RoutingContext(
                        openReq.Model ?? "default",
                        resolvedModel?.ToString() ?? string.Empty,
                        provider?.ToString() ?? string.Empty);
                }

                yield return new Microsoft.Agents.AI.AgentResponseUpdate
                {
                    Role = translatedUpdate.Role,
                    Contents = translatedUpdate.Contents,
                    AdditionalProperties = translatedUpdate.AdditionalProperties,
                };
            }
        }

        /// <inheritdoc/>
        public override ValueTask<AgentSession> CreateSessionAsync(CancellationToken cancellationToken = default)
            => new ValueTask<AgentSession>(new RoutingAgentSession());

        /// <inheritdoc/>
        public override ValueTask<AgentSession> DeserializeSessionAsync(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
            => new ValueTask<AgentSession>(new RoutingAgentSession());

        /// <inheritdoc/>
        public override JsonElement SerializeSession(AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null)
            => JsonSerializer.SerializeToElement(new { }, jsonSerializerOptions);
    }
}
