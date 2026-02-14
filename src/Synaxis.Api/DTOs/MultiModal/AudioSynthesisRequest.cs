// <copyright file="AudioSynthesisRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a request for text-to-speech synthesis.
    /// </summary>
    public class AudioSynthesisRequest
    {
        /// <summary>
        /// Gets or sets the text to synthesize.
        /// </summary>
        [Required]
        public string Input { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model to use for synthesis.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the voice to use for synthesis.
        /// </summary>
        public string Voice { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response format (mp3, opus, aac, flac, wav, pcm).
        /// </summary>
        public string ResponseFormat { get; set; } = "mp3";

        /// <summary>
        /// Gets or sets the speed of the synthesized audio (0.25 to 4.0).
        /// </summary>
        [Range(0.25, 4.0)]
        public double Speed { get; set; } = 1.0;
    }
}
