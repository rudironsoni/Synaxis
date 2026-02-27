// <copyright file="ICommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Commands
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Marker interface for commands that produce a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response produced by the command.</typeparam>
#pragma warning disable S2326 // Unused type parameter - intentional marker interface
    public interface ICommand<out TResponse>
    {
    }
#pragma warning restore S2326
}
