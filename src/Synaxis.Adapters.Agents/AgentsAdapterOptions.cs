// <copyright file="AgentsAdapterOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Agents
{
    /// <summary>
    /// Configuration options for the Synaxis Agents adapter.
    /// </summary>
    public class AgentsAdapterOptions
    {
        /// <summary>
        /// Gets or sets the Microsoft App ID for Bot Framework authentication.
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Microsoft App Password for Bot Framework authentication.
        /// </summary>
        public string AppPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether authentication should be used.
        /// Set to false for local development without authentication.
        /// </summary>
        public bool UseAuthentication { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of messages to keep in conversation history.
        /// Default is 20 messages.
        /// </summary>
        public int MaxHistoryMessages { get; set; } = 20;

        /// <summary>
        /// Gets or sets the default model to use for chat completions.
        /// Default is "gpt-4".
        /// </summary>
        public string DefaultModel { get; set; } = "gpt-4";

        /// <summary>
        /// Gets or sets a value indicating whether to enable detailed error messages in responses.
        /// Should be false in production.
        /// </summary>
        public bool IncludeDetailedErrors { get; set; } = false;
    }
}
