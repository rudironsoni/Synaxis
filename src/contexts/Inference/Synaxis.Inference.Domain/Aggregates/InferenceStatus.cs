// <copyright file="InferenceStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents the status of an inference request.
/// </summary>
public enum InferenceStatus
{
    /// <summary>
    /// Request is pending routing.
    /// </summary>
    Pending,

    /// <summary>
    /// Request is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Request completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Request failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Request was cancelled.
    /// </summary>
    Cancelled,
}
