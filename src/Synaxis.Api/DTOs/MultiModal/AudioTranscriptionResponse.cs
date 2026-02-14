// <copyright file="AudioTranscriptionResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    /// <summary>
    /// Represents a response from audio transcription.
    /// </summary>
    public class AudioTranscriptionResponse
    {
        /// <summary>
        /// Gets or sets the transcribed text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the task type (always "transcribe").
        /// </summary>
        public string Task { get; set; } = "transcribe";

        /// <summary>
        /// Gets or sets the language detected.
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duration of the audio in seconds.
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// Gets or sets the words with timestamps (for verbose_json format).
        /// </summary>
        public IList<TimestampedWord> Words { get; set; } = new List<TimestampedWord>();

        /// <summary>
        /// Represents a word with timestamp information.
        /// </summary>
        public class TimestampedWord
        {
            /// <summary>
            /// Gets or sets the word text.
            /// </summary>
            public string Word { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the start time in seconds.
            /// </summary>
            public double Start { get; set; }

            /// <summary>
            /// Gets or sets the end time in seconds.
            /// </summary>
            public double End { get; set; }
        }
    }
}
