// <copyright file="SagaStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain;

/// <summary>
/// Represents the status of a saga.
/// </summary>
public enum SagaStatus
{
    /// <summary>
    /// Saga is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Saga is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Saga completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Saga failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Saga is compensating due to failure.
    /// </summary>
    Compensating,

    /// <summary>
    /// Saga was fully compensated.
    /// </summary>
    Compensated,

    /// <summary>
    /// Saga was cancelled.
    /// </summary>
    Cancelled,
}
