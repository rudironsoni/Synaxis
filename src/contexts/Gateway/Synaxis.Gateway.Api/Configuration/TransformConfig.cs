// <copyright file="TransformConfig.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Represents transform configuration.
/// </summary>
public class TransformConfig
{
    /// <summary>
    /// Gets or sets the transform type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transform values.
    /// </summary>
    public IDictionary<string, string> Values { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
