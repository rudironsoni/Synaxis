// <copyright file="ImageGenerationResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents an image generation response with created timestamp and data array.
    /// </summary>
    public sealed class ImageGenerationResponse
    {
        /// <summary>
        /// Gets or initializes the Unix timestamp when these images were created.
        /// </summary>
        public long Created { get; init; }

        /// <summary>
        /// Gets or initializes the array of generated image data.
        /// </summary>
        public ImageData[] Data { get; init; } = System.Array.Empty<ImageData>();
    }
}
