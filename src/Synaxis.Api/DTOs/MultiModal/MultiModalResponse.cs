// <copyright file="MultiModalResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a multi-modal response compatible with OpenAI format.
    /// </summary>
    public class MultiModalResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the response.
        /// </summary>
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the object type (always "chat.completion").
        /// </summary>
        public string Object { get; set; } = "chat.completion";

        /// <summary>
        /// Gets or sets the timestamp of the response.
        /// </summary>
        public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// Gets or sets the model used for the completion.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of choices in the response.
        /// </summary>
        public IList<Choice> Choices { get; set; } = new List<Choice>();

        /// <summary>
        /// Gets or sets the usage statistics.
        /// </summary>
        public Usage Usage { get; set; } = new();
    }
}
