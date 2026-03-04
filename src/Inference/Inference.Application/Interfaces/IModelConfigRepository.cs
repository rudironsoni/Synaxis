// <copyright file="IModelConfigRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Interfaces;

using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Repository interface for model configuration operations.
/// </summary>
public interface IModelConfigRepository
{
    /// <summary>
    /// Gets a model configuration by its identifier.
    /// </summary>
    /// <param name="id">The configuration identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The model configuration if found; otherwise null.</returns>
    Task<ModelConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets model configurations by tenant identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeInactive">Whether to include inactive configurations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of model configurations.</returns>
    Task<IReadOnlyList<ModelConfig>> GetByTenantAsync(
        Guid tenantId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a model configuration by model and provider identifiers.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The model configuration if found; otherwise null.</returns>
    Task<ModelConfig?> GetByModelAndProviderAsync(
        Guid tenantId,
        string modelId,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active model configurations by provider.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of model configurations.</returns>
    Task<IReadOnlyList<ModelConfig>> GetByProviderAsync(
        Guid tenantId,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active model configurations by capability.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="capability">The capability name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of model configurations.</returns>
    Task<IReadOnlyList<ModelConfig>> GetByCapabilityAsync(
        Guid tenantId,
        string capability,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new model configuration.
    /// </summary>
    /// <param name="config">The model configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(ModelConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing model configuration.
    /// </summary>
    /// <param name="config">The model configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ModelConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model configuration exists for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="excludeId">Optional configuration identifier to exclude from check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the configuration exists; otherwise false.</returns>
    Task<bool> ExistsAsync(
        Guid tenantId,
        string modelId,
        string providerId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
