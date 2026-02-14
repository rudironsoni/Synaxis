// <copyright file="OpenAiErrorDetail.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    /// <summary>
    /// Represents OpenAI error details.
    /// </summary>
    internal sealed class OpenAiErrorDetail
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter that caused the error.
        /// </summary>
        public string Param { get; set; } = null!;
    }
}
