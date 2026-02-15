// <copyright file="BatchStorageOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services;

/// <summary>
/// Configuration options for batch storage.
/// </summary>
public class BatchStorageOptions
{
    /// <summary>
    /// Gets or sets the Azure Blob Storage connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName { get; set; } = "batch-processing";
}
