// <copyright file="RerankResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents a rerank response with results array and optional usage statistics.
    /// </summary>
    public sealed class RerankResponse
    {
        /// <summary>
        /// Gets or initializes the array of rerank results.
        /// </summary>
        public RerankResult[] Results { get; init; } = System.Array.Empty<RerankResult>();

        /// <summary>
        /// Gets or initializes the usage statistics for this rerank request.
        /// </summary>
        public EmbeddingUsage? Usage { get; init; }
    }
}
