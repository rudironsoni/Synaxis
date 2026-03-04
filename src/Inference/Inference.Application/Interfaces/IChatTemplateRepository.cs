// <copyright file="IChatTemplateRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Interfaces;

using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Repository interface for chat template operations.
/// </summary>
public interface IChatTemplateRepository
{
    /// <summary>
    /// Gets a chat template by its identifier.
    /// </summary>
    /// <param name="id">The template identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chat template if found; otherwise null.</returns>
    Task<ChatTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets chat templates by tenant identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="includeInactive">Whether to include inactive templates.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of chat templates.</returns>
    Task<IReadOnlyList<ChatTemplate>> GetByTenantAsync(
        Guid tenantId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets chat templates by category.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="category">The category.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of chat templates.</returns>
    Task<IReadOnlyList<ChatTemplate>> GetByCategoryAsync(
        Guid tenantId,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system templates.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of system templates.</returns>
    Task<IReadOnlyList<ChatTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shared templates accessible to a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of shared templates.</returns>
    Task<IReadOnlyList<ChatTemplate>> GetSharedTemplatesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new chat template.
    /// </summary>
    /// <param name="template">The chat template.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(ChatTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing chat template.
    /// </summary>
    /// <param name="template">The chat template.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ChatTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a template name exists for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="name">The template name.</param>
    /// <param name="excludeId">Optional template identifier to exclude from check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the name exists; otherwise false.</returns>
    Task<bool> NameExistsAsync(
        Guid tenantId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
