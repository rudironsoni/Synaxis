// <copyright file="IChatCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands
{
    using Synaxis.Abstractions.Commands;

    /// <summary>
    /// Marker interface for chat completion commands that produce a <see cref="Messages.ChatResponse"/>.
    /// </summary>
    /// <typeparam name="TChatResponse">The type of chat response produced by the command.</typeparam>
    public interface IChatCommand<out TChatResponse> : ICommand<TChatResponse>
    {
    }
}
