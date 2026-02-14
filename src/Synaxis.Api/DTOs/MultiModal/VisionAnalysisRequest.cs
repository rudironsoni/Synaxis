// <copyright file="VisionAnalysisRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a request for image analysis.
    /// </summary>
    public class VisionAnalysisRequest
    {
        /// <summary>
        /// Gets or sets the image content (base64 encoded or URL).
        /// </summary>
        [Required]
        public ContentPart Image { get; set; } = new();

        /// <summary>
        /// Gets or sets the prompt/question about the image.
        /// </summary>
        [Required]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum detail level (low, high, auto).
        /// </summary>
        public string Detail { get; set; } = "auto";

        /// <summary>
        /// Gets or sets the model to use for vision analysis.
        /// </summary>
        public string Model { get; set; } = string.Empty;
    }
}
