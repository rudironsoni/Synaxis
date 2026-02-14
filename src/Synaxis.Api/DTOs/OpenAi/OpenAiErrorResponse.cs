// <copyright file="OpenAiErrorResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    /// <summary>
    /// Represents an OpenAI-compatible error response.
    /// </summary>
    internal sealed class OpenAiErrorResponse
    {
        /// <summary>
        /// Gets or sets the error details.
        /// </summary>
        public OpenAiErrorDetail Error { get; set; } = new OpenAiErrorDetail();
    }
}
