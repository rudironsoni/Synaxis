// <copyright file="IControlPlaneStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    /// <summary>
    /// Store for control plane data access.
    /// </summary>
    public interface IControlPlaneStore
    {
        /// <summary>
        /// Gets a model alias by tenant ID and alias name.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="alias">The alias name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The model alias if found, otherwise null.</returns>
        Task<ModelAlias?> GetAliasAsync(Guid tenantId, string alias, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a model combo by tenant ID and combo name.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="name">The combo name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The model combo if found, otherwise null.</returns>
        Task<ModelCombo?> GetComboAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a global model by ID.
        /// </summary>
        /// <param name="id">The model ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The global model if found, otherwise null.</returns>
        Task<GlobalModel?> GetGlobalModelAsync(string id, CancellationToken cancellationToken = default);
    }
}
