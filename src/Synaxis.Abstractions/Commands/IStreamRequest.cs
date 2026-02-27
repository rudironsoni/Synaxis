// <copyright file="IStreamRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Commands
{
    /// <summary>
    /// Marker interface for requests that produce streaming responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of response produced by the stream.</typeparam>
#pragma warning disable S2326 // Unused type parameter - intentional marker interface
    public interface IStreamRequest<out TResponse>
    {
    }
#pragma warning restore S2326
}
