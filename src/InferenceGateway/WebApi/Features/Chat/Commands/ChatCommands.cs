// <copyright file="ChatCommands.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Features.Chat.Commands
{
    using System.Collections.Generic;
    using Mediator;
    using Microsoft.Agents.AI;
    using Microsoft.Extensions.AI;
    using Synaxis.InferenceGateway.Application.Translation;

    /// <summary>
    /// Command for non-streaming chat completion.
    /// </summary>
    /// <param name="request">The OpenAI request.</param>
    /// <param name="messages">The chat messages.</param>
    public record ChatCommand(OpenAIRequest request, IEnumerable<ChatMessage> messages)
    : IRequest<Microsoft.Agents.AI.AgentResponse>;

    /// <summary>
    /// Command for streaming chat completion.
    /// </summary>
    /// <param name="request">The OpenAI request.</param>
    /// <param name="messages">The chat messages.</param>
    public record ChatStreamCommand(OpenAIRequest request, IEnumerable<ChatMessage> messages)
    : IStreamRequest<Microsoft.Agents.AI.AgentResponseUpdate>;
}
