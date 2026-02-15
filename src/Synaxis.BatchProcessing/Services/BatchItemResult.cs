// <copyright file="BatchItemResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services;

using Synaxis.BatchProcessing.Models;

/// <summary>
/// Represents the result of processing a batch item.
/// </summary>
internal class BatchItemResult
{
    /// <summary>
    /// Gets or sets the item identifier.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item status.
    /// </summary>
    public BatchItemStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the result data.
    /// </summary>
    public object Result { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
