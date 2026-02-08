// <copyright file="ImageData.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents generated image data with URL, base64-encoded data, or revised prompt.
    /// </summary>
    public sealed class ImageData
    {
        /// <summary>
        /// Gets or initializes the URL of the generated image.
        /// </summary>
        public string? Url { get; init; }

        /// <summary>
        /// Gets or initializes the base64-encoded JSON representation of the image.
        /// </summary>
        public string? B64Json { get; init; }

        /// <summary>
        /// Gets or initializes the revised prompt that was used to generate the image.
        /// </summary>
        public string? RevisedPrompt { get; init; }
    }
}
