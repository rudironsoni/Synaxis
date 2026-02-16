// <copyright file="ApiKeyAuthenticationOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Authentication
{
    using Microsoft.AspNetCore.Authentication;

    /// <summary>
    /// Options for API key authentication.
    /// </summary>
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Gets or sets the API key header name.
        /// </summary>
        public string HeaderName { get; set; } = "Authorization";
    }
}
