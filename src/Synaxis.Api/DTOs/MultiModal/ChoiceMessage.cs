// <copyright file="ChoiceMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    /// <summary>
    /// Represents a message in the choice.
    /// </summary>
    public class ChoiceMessage
    {
        /// <summary>
        /// Gets or sets the role of the message sender.
        /// </summary>
        public string Role { get; set; } = "assistant";

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}
