// <copyright file="IApiKeyCredential.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Auth
{
    /// <summary>
    /// Defines a contract for API key-based credentials.
    /// </summary>
    public interface IApiKeyCredential
    {
        /// <summary>
        /// Gets the API key.
        /// </summary>
        string Key { get; }
    }
}
