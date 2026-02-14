// <copyright file="ContentPart.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a part of multi-modal content.
    /// </summary>
    public class ContentPart
    {
        /// <summary>
        /// Gets or sets the type of content (text, image, audio).
        /// </summary>
        [Required]
        public string Type { get; set; } = "text";

        /// <summary>
        /// Gets or sets the text content (for text type).
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL of the media resource (for image/audio type).
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base64 encoded content (for image/audio type).
        /// </summary>
        public string Base64 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the media type (e.g., "image/png", "audio/wav").
        /// </summary>
        public string MediaType { get; set; } = string.Empty;
    }
}
