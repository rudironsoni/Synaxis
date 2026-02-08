// <copyright file="AudioSynthesis.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents synthesized audio data with content type information.
    /// </summary>
    public sealed class AudioSynthesis
    {
        /// <summary>
        /// Gets or initializes the audio data as a byte array.
        /// </summary>
        public byte[] AudioData { get; init; } = System.Array.Empty<byte>();

        /// <summary>
        /// Gets or initializes the content type of the audio (e.g., "audio/mpeg", "audio/wav").
        /// </summary>
        public string ContentType { get; init; } = "audio/mpeg";
    }
}
