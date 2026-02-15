// <copyright file="ResourceLimits.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

/// <summary>
/// Represents resource limits for a container.
/// </summary>
public class ResourceLimits
{
    /// <summary>
    /// Gets or sets the CPU limit in millicores (e.g., 500 for 0.5 CPU).
    /// </summary>
    public int? CpuLimitMillicores { get; set; }

    /// <summary>
    /// Gets or sets the memory limit in megabytes.
    /// </summary>
    public int? MemoryLimitMB { get; set; }
}
