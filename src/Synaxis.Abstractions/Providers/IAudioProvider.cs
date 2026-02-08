// <copyright file="IAudioProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Providers
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for providers that support audio operations.
    /// </summary>
    public interface IAudioProvider : IProviderClient
    {
        /// <summary>
        /// Transcribes audio to text asynchronously.
        /// </summary>
        /// <param name="audioData">The audio data to transcribe.</param>
        /// <param name="model">The model to use for transcription.</param>
        /// <param name="options">Optional transcription parameters.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the transcription response.</returns>
        Task<object> TranscribeAsync(
            byte[] audioData,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Synthesizes text to audio asynchronously.
        /// </summary>
        /// <param name="text">The text to synthesize.</param>
        /// <param name="model">The model to use for synthesis.</param>
        /// <param name="options">Optional synthesis parameters.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the synthesis response.</returns>
        Task<object> SynthesizeAsync(
            string text,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default);
    }
}
