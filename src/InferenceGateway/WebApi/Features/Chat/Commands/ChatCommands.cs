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
    /// <param name="Request">The OpenAI request.</param>
    /// <param name="Messages">The chat messages.</param>
    public record ChatCommand(OpenAIRequest Request, IEnumerable<ChatMessage> Messages)
    : IRequest<Microsoft.Agents.AI.AgentResponse>;

    /// <summary>
    /// Command for streaming chat completion.
    /// </summary>
    /// <param name="Request">The OpenAI request.</param>
    /// <param name="Messages">The chat messages.</param>
    public record ChatStreamCommand(OpenAIRequest Request, IEnumerable<ChatMessage> Messages)
    : IStreamRequest<Microsoft.Agents.AI.AgentResponseUpdate>;
}
