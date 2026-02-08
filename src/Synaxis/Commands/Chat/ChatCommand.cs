// <copyright file="ChatCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Commands.Chat
{
    using Mediator;
    using Synaxis.Contracts.V1.Commands;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Represents a chat completion command.
    /// </summary>
    /// <param name="Messages">The conversation messages.</param>
    /// <param name="Model">The model to use for completion.</param>
    /// <param name="Temperature">The sampling temperature (0.0-2.0).</param>
    /// <param name="MaxTokens">The maximum number of tokens to generate.</param>
    /// <param name="Provider">Optional provider name override.</param>
    public sealed record ChatCommand(
        ChatMessage[] Messages,
        string Model,
        double? Temperature = null,
        int? MaxTokens = null,
        string? Provider = null) : IChatCommand<ChatResponse>, IRequest<ChatResponse>;
}
