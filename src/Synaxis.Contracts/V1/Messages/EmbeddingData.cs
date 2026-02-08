// <copyright file="EmbeddingData.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents a single embedding result with index, vector data, and object type.
    /// </summary>
    public sealed class EmbeddingData
    {
        /// <summary>
        /// Gets or initializes the index of this embedding in the list of embeddings.
        /// </summary>
        public int Index { get; init; }

        /// <summary>
        /// Gets or initializes the embedding vector as an array of floats.
        /// </summary>
        public float[] Embedding { get; init; } = System.Array.Empty<float>();

        /// <summary>
        /// Gets or initializes the object type (typically "embedding").
        /// </summary>
        public string Object { get; init; } = "embedding";
    }
}
