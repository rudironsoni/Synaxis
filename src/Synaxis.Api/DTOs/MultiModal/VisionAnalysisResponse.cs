// <copyright file="VisionAnalysisResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    /// <summary>
    /// Represents a response from image analysis.
    /// </summary>
    public class VisionAnalysisResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the response.
        /// </summary>
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the object type (always "vision.analysis").
        /// </summary>
        public string Object { get; set; } = "vision.analysis";

        /// <summary>
        /// Gets or sets the timestamp of the response.
        /// </summary>
        public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// Gets or sets the model used for analysis.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the analysis result.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the usage statistics.
        /// </summary>
        public Usage Usage { get; set; } = new();
    }
}
