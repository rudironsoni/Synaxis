// <copyright file="IDeviationRegistry.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    /// <summary>
    /// Registry for tracking API specification deviations.
    /// </summary>
    public interface IDeviationRegistry
    {
        /// <summary>
        /// Registers a new deviation entry.
        /// </summary>
        /// <param name="entry">The deviation entry to register.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RegisterAsync(DeviationEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all deviation entries for a tenant.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A read-only list of deviation entries.</returns>
        Task<IReadOnlyList<DeviationEntry>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the status of a deviation entry.
        /// </summary>
        /// <param name="deviationId">The deviation ID.</param>
        /// <param name="status">The new status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateStatusAsync(Guid deviationId, DeviationStatus status, CancellationToken cancellationToken = default);
    }
}
