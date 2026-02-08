// <copyright file="AudioTranscriptionResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents an audio transcription response with text, duration, and language.
    /// </summary>
    public sealed class AudioTranscriptionResponse
    {
        /// <summary>
        /// Gets or initializes the transcribed text.
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the duration of the audio in seconds.
        /// </summary>
        public double? Duration { get; init; }

        /// <summary>
        /// Gets or initializes the detected or specified language of the audio.
        /// </summary>
        public string? Language { get; init; }
    }
}
