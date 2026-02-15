// <copyright file="StampLifecycleOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.StampController.Services;

using Microsoft.Extensions.Options;

/// <summary>
/// Configuration options for stamp lifecycle.
/// </summary>
public class StampLifecycleOptions
{
    /// <summary>
    /// Gets or sets the delay in milliseconds for the Provision phase.
    /// </summary>
    public int ProvisionDelayMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the delay in milliseconds for the Register phase.
    /// </summary>
    public int RegisterDelayMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the delay in milliseconds for the Drain phase.
    /// </summary>
    public int DrainDelayMs { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the delay in milliseconds for the Quarantine phase.
    /// </summary>
    public int QuarantineDelayMs { get; set; } = 300000; // 5 minutes

    /// <summary>
    /// Gets or sets the delay in milliseconds for the Decommission phase.
    /// </summary>
    public int DecommissionDelayMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the delay in milliseconds for the Archive phase.
    /// </summary>
    public int ArchiveDelayMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the delay in milliseconds for the Purge phase.
    /// </summary>
    public int PurgeDelayMs { get; set; } = 5000;
}
