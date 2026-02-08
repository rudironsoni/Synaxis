// <copyright file="EmbeddingCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Commands.Embeddings
{
    using Mediator;
    using Synaxis.Contracts.V1.Commands;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// Represents an embedding generation command.
    /// </summary>
    /// <param name="Input">The input text(s) to embed.</param>
    /// <param name="Model">The embedding model to use.</param>
    /// <param name="Provider">Optional provider name override.</param>
    public sealed record EmbeddingCommand(
        string[] Input,
        string Model,
        string? Provider = null) : IEmbeddingCommand<EmbeddingResponse>, IRequest<EmbeddingResponse>;
}
