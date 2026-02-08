// <copyright file="IRerankCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands
{
    using Synaxis.Abstractions.Commands;

    /// <summary>
    /// Marker interface for rerank commands that produce a <see cref="Messages.RerankResponse"/>.
    /// </summary>
    /// <typeparam name="TRerankResponse">The type of rerank response produced by the command.</typeparam>
    public interface IRerankCommand<out TRerankResponse> : ICommand<TRerankResponse>
    {
    }
}
