// <copyright file="MultiModalRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a multi-modal request for chat completions.
    /// </summary>
    public class MultiModalRequest
    {
        /// <summary>
        /// Gets or sets the list of messages in the conversation.
        /// </summary>
        [Required]
        public IList<Message> Messages { get; set; } = new List<Message>();

        /// <summary>
        /// Gets or sets the model to use for completion.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the temperature for sampling (0.0 to 2.0).
        /// </summary>
        [Range(0.0, 2.0)]
        public double Temperature { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate.
        /// </summary>
        [Range(1, 4096)]
        public int MaxTokens { get; set; } = 256;

        /// <summary>
        /// Gets or sets the sampling strategy (nucleus sampling).
        /// </summary>
        [Range(0.0, 1.0)]
        public double TopP { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the frequency penalty (-2.0 to 2.0).
        /// </summary>
        [Range(-2.0, 2.0)]
        public double FrequencyPenalty { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the presence penalty (-2.0 to 2.0).
        /// </summary>
        [Range(-2.0, 2.0)]
        public double PresencePenalty { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets a value indicating whether to stream the response.
        /// </summary>
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Represents a message in the conversation.
        /// </summary>
        public class Message
        {
            /// <summary>
            /// Gets or sets the role of the message sender (system, user, assistant).
            /// </summary>
            [Required]
            public string Role { get; set; } = "user";

            /// <summary>
            /// Gets or sets the content parts of the message.
            /// </summary>
            [Required]
            public IList<ContentPart> Content { get; set; } = new List<ContentPart>();
        }
    }
}
