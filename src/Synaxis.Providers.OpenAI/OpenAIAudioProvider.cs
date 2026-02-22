// <copyright file="OpenAIAudioProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI
{
    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Providers;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// OpenAI implementation of <see cref="IAudioProvider"/> using Whisper.
    /// </summary>
    public sealed class OpenAIAudioProvider : IAudioProvider
    {
        private readonly OpenAIClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIAudioProvider"/> class.
        /// </summary>
        /// <param name="client">The OpenAI client.</param>
        /// <param name="logger">The logger.</param>
        public OpenAIAudioProvider(
            OpenAIClient client,
            ILogger<OpenAIAudioProvider> logger)
        {
            this._client = client!;
            _ = logger!;
        }

        /// <inheritdoc/>
        public string ProviderName => "OpenAI";

        /// <inheritdoc/>
        public async Task<object> TranscribeAsync(
            byte[] audioData,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(audioData);
            ArgumentNullException.ThrowIfNull(model);

            if (audioData.Length == 0)
            {
                throw new ArgumentException("Audio data cannot be empty", nameof(audioData));
            }

            using var formData = new MultipartFormDataContent();
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
            formData.Add(audioContent, "file", "audio.mp3");
            formData.Add(new StringContent(model), "model");

            var response = await this._client.PostMultipartAsync<JsonDocument>(
                "audio/transcriptions",
                formData,
                cancellationToken).ConfigureAwait(false);

            return this.MapToSynaxisResponse(response);
        }

        /// <inheritdoc/>
        public Task<object> SynthesizeAsync(
            string text,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            // OpenAI TTS is available but not implemented in this initial version
            throw new NotSupportedException("Audio synthesis is not yet implemented for OpenAI provider");
        }

        private AudioTranscriptionResponse MapToSynaxisResponse(JsonDocument response)
        {
            var root = response.RootElement;
            var text = root.GetProperty("text").GetString() ?? string.Empty;

            double? duration = null;
            if (root.TryGetProperty("duration", out var durationElement))
            {
                duration = durationElement.GetDouble();
            }

            string? language = null;
            if (root.TryGetProperty("language", out var languageElement))
            {
                language = languageElement.GetString();
            }

            return new AudioTranscriptionResponse
            {
                Text = text,
                Duration = duration,
                Language = language,
            };
        }
    }
}
