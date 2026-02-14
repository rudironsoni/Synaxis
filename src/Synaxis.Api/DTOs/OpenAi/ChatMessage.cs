// <copyright file="ChatMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a chat message.
    /// </summary>
    public sealed class ChatMessage
    {
        /// <summary>
        /// Gets or sets the role of the message author.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the contents of the message.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the message author.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }
}
