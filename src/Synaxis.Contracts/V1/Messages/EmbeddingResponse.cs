// <copyright file="EmbeddingResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents an embedding response with object type, data array, and usage statistics.
    /// </summary>
    public sealed class EmbeddingResponse
    {
        /// <summary>
        /// Gets or initializes the object type (typically "list").
        /// </summary>
        public string Object { get; init; } = "list";

        /// <summary>
        /// Gets or initializes the array of embedding data results.
        /// </summary>
        public EmbeddingData[] Data { get; init; } = System.Array.Empty<EmbeddingData>();

        /// <summary>
        /// Gets or initializes the usage statistics for this embedding request.
        /// </summary>
        public EmbeddingUsage? Usage { get; init; }
    }
}
