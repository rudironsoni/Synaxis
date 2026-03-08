// <copyright file="IStreamRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Application.Commands
{
    /// <summary>
    /// Marker interface for requests that produce streaming responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of response produced by the stream.</typeparam>
    public interface IStreamRequest<out TResponse>
    {
        /// <summary>
        /// Gets the response payload for the stream.
        /// </summary>
        TResponse Response => default!;
    }
}
