// <copyright file="ICommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Application.Commands
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Marker interface for commands that produce a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response produced by the command.</typeparam>
    public interface ICommand<out TResponse>
    {
        /// <summary>
        /// Gets the response payload for the command.
        /// </summary>
        TResponse Response => default!;
    }
}
