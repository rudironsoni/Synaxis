// <copyright file="ChatCompletionResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a chat completion response compatible with OpenAI API.
    /// </summary>
    public sealed class ChatCompletionResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the chat completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion";

        /// <summary>
        /// Gets or sets the Unix timestamp when the chat completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model used for the chat completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of chat completion choices.
        /// </summary>
        [JsonPropertyName("choices")]
        public IList<ChatChoice> Choices { get; set; } = new List<ChatChoice>();

        /// <summary>
        /// Gets or sets the usage statistics.
        /// </summary>
        [JsonPropertyName("usage")]
        public Usage Usage { get; set; } = null!;

        /// <summary>
        /// Gets or sets the system fingerprint.
        /// </summary>
        [JsonPropertyName("system_fingerprint")]
        public string SystemFingerprint { get; set; } = null!;
    }
}
