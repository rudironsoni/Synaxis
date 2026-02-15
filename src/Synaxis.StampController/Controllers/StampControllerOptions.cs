// <copyright file="StampControllerOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.StampController.Controllers;

using Microsoft.Extensions.Options;

/// <summary>
/// Configuration options for the stamp controller.
/// </summary>
public class StampControllerOptions
{
    /// <summary>
    /// Gets or sets the interval in milliseconds between processing cycles.
    /// </summary>
    public int ProcessingIntervalMs { get; set; } = 30000; // 30 seconds
}
