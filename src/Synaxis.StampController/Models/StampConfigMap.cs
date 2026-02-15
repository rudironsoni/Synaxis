// <copyright file="StampConfigMap.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.StampController.Models;

using System.Collections.Generic;

/// <summary>
/// Represents stamp data stored in a Kubernetes ConfigMap.
/// </summary>
public class StampConfigMap
{
    /// <summary>
    /// Gets or sets the unique identifier for the stamp.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the stamp.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current phase of the stamp lifecycle.
    /// </summary>
    public StampPhase Phase { get; set; }

    /// <summary>
    /// Gets or sets the region where the stamp is deployed.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the stamp.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the stamp was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the time-to-live (TTL) for the stamp in seconds.
    /// </summary>
    public int? Ttl { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the stamp.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
