// <copyright file="AuthOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Represents authentication options.
/// </summary>
public class AuthOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether authentication is enabled globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the authentication schemes.
    /// </summary>
    public IList<string> Schemes { get; set; } = new List<string> { "Bearer" };

    /// <summary>
    /// Gets or sets the token validation options.
    /// </summary>
    public TokenValidationOptions TokenValidation { get; set; } = new();
}
