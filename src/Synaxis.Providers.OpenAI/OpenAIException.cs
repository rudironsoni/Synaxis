// <copyright file="OpenAIException.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI
{
    using System;
    using Synaxis.Contracts.V1.Errors;

    /// <summary>
    /// Exception thrown when an OpenAI API error occurs.
    /// </summary>
    public sealed class OpenAIException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIException"/> class.
        /// </summary>
        /// <param name="error">The Synaxis error.</param>
        public OpenAIException(SynaxisError error)
            : base(error?.Message ?? "OpenAI API error")
        {
            this.Error = error!;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIException"/> class.
        /// </summary>
        /// <param name="error">The Synaxis error.</param>
        /// <param name="innerException">The inner exception.</param>
        public OpenAIException(SynaxisError error, Exception? innerException)
            : base(error?.Message ?? "OpenAI API error", innerException)
        {
            this.Error = error!;
        }

        /// <summary>
        /// Gets the Synaxis error.
        /// </summary>
        public SynaxisError Error { get; }
    }
}
