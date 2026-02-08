// <copyright file="AudioSynthesisResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents an audio synthesis response with audio data and content type.
    /// </summary>
    public sealed class AudioSynthesisResponse
    {
        /// <summary>
        /// Gets or initializes the synthesized audio data as a byte array.
        /// </summary>
        public byte[] AudioData { get; init; } = System.Array.Empty<byte>();

        /// <summary>
        /// Gets or initializes the content type of the audio (e.g., "audio/mpeg", "audio/wav").
        /// </summary>
        public string ContentType { get; init; } = "audio/mpeg";
    }
}
