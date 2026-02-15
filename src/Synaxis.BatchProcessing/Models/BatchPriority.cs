// <copyright file="BatchPriority.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Models;

/// <summary>
/// Represents the priority of a batch.
/// </summary>
public enum BatchPriority
{
    /// <summary>
    /// Low priority batch.
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority batch.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority batch.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority batch.
    /// </summary>
    Critical,
}
