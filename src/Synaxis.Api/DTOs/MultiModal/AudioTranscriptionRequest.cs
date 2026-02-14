// <copyright file="AudioTranscriptionRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a request for audio transcription.
    /// </summary>
    public class AudioTranscriptionRequest
    {
        /// <summary>
        /// Gets or sets the audio content (base64 encoded or URL).
        /// </summary>
        [Required]
        public ContentPart Audio { get; set; } = new();

        /// <summary>
        /// Gets or sets the model to use for transcription.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the language of the audio (ISO 639-1 code).
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prompt to guide the transcription.
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response format (json, text, srt, verbose_json, vtt).
        /// </summary>
        public string ResponseFormat { get; set; } = "json";

        /// <summary>
        /// Gets or sets the temperature for sampling (0.0 to 1.0).
        /// </summary>
        [Range(0.0, 1.0)]
        public double Temperature { get; set; } = 0.0;
    }
}
