// <copyright file="ProviderException.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Exceptions
{
    using System;
    using Synaxis.Contracts.V1.Errors;

    /// <summary>
    /// Exception thrown when a provider operation fails.
    /// </summary>
    public sealed class ProviderException : Exception
    {
        /// <summary>
        /// Gets the error details.
        /// </summary>
        public SynaxisError Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderException"/> class.
        /// </summary>
        /// <param name="error">The error details.</param>
        public ProviderException(SynaxisError error)
            : base(error.Message)
        {
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderException"/> class.
        /// </summary>
        /// <param name="error">The error details.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProviderException(SynaxisError error, Exception innerException)
            : base(error.Message, innerException)
        {
            this.Error = error;
        }
    }
}
