// <copyright file="IAuthenticationContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Auth
{
    /// <summary>
    /// Defines a contract for authentication context information.
    /// </summary>
    public interface IAuthenticationContext
    {
        /// <summary>
        /// Gets the authentication scheme.
        /// </summary>
        string Scheme { get; }

        /// <summary>
        /// Gets a value indicating whether the context is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}
