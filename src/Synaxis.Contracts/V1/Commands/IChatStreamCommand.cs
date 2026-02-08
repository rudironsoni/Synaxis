// <copyright file="IChatStreamCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands
{
    using Synaxis.Abstractions.Commands;

    /// <summary>
    /// Marker interface for streaming chat completion commands that produce <see cref="Messages.ChatStreamChunk"/> responses.
    /// </summary>
    /// <typeparam name="TChatStreamChunk">The type of chat stream chunk produced by the command.</typeparam>
    public interface IChatStreamCommand<out TChatStreamChunk> : IStreamRequest<TChatStreamChunk>
    {
    }
}
