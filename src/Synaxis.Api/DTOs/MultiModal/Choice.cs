// <copyright file="Choice.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    /// <summary>
    /// Represents a choice in the response.
    /// </summary>
    public class Choice
    {
        /// <summary>
        /// Gets or sets the index of the choice.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public ChoiceMessage Message { get; set; } = new();

        /// <summary>
        /// Gets or sets the reason the response finished.
        /// </summary>
        public string FinishReason { get; set; } = string.Empty;
    }
}
