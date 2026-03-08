// <copyright file="IEmbeddingCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Commands
{
    using Synaxis.Shared.Kernel.Application.Commands;

    /// <summary>
    /// Marker interface for embedding generation commands that produce an <see cref="Messages.EmbeddingResponse"/>.
    /// </summary>
    /// <typeparam name="TEmbeddingResponse">The type of embedding response produced by the command.</typeparam>
    public interface IEmbeddingCommand<TEmbeddingResponse> : ICommand<TEmbeddingResponse>
    {
    }
}
