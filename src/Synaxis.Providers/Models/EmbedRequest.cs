// <copyright file="EmbedRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Models
{
    /// <summary>
    /// Represents an embedding request.
    /// </summary>
    public sealed class EmbedRequest
    {
        /// <summary>
        /// Gets or initializes the input texts to embed.
        /// </summary>
        public string[] Input { get; init; } = System.Array.Empty<string>();

        /// <summary>
        /// Gets or initializes the model to use.
        /// </summary>
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the encoding format.
        /// </summary>
        public string? EncodingFormat { get; init; }

        /// <summary>
        /// Gets or initializes the dimensions for the embedding.
        /// </summary>
        public int? Dimensions { get; init; }
    }
}
