// <copyright file="TokenValidationOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Represents token validation options.
/// </summary>
public class TokenValidationOptions
{
    /// <summary>
    /// Gets or sets the authority URL.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets the audience.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to validate issuer.
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate audience.
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate lifetime.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;
}
