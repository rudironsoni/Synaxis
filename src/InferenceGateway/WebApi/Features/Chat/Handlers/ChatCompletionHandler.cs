// <copyright file="ChatCompletionHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Features.Chat.Handlers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Mediator;
    using Microsoft.Agents.AI;
    using Synaxis.InferenceGateway.WebApi.Agents;
    using Synaxis.InferenceGateway.WebApi.Features.Chat.Commands;

    /// <summary>
    /// Handler for chat completion requests.
    /// </summary>
    public class ChatCompletionHandler :
    IRequestHandler<ChatCommand, Microsoft.Agents.AI.AgentResponse>,
    IStreamRequestHandler<ChatStreamCommand, Microsoft.Agents.AI.AgentResponseUpdate>
    {
        private readonly RoutingAgent _agent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatCompletionHandler"/> class.
        /// </summary>
        /// <param name="agent">The routing agent.</param>
        public ChatCompletionHandler(RoutingAgent agent)
        {
            this._agent = agent;
        }

        /// <summary>
        /// Handles a non-streaming chat completion request.
        /// </summary>
        /// <param name="request">The chat command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The agent response.</returns>
        public async ValueTask<AgentResponse> Handle(ChatCommand request, CancellationToken cancellationToken)
        {
            return await this._agent.RunAsync(request.messages, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles a streaming chat completion request.
        /// </summary>
        /// <param name="request">The chat stream command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of agent response updates.</returns>
        public IAsyncEnumerable<AgentResponseUpdate> Handle(ChatStreamCommand request, CancellationToken cancellationToken)
        {
            return this._agent.RunStreamingAsync(request.messages, cancellationToken: cancellationToken);
        }
    }
}
