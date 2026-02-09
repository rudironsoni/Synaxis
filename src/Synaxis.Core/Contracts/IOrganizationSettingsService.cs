// <copyright file="IOrganizationSettingsService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing organization settings and limits.
    /// </summary>
    public interface IOrganizationSettingsService
    {
        /// <summary>
        /// Gets the organization limits.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The organization limits response.</returns>
        Task<OrganizationLimitsResponse> GetOrganizationLimitsAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the organization limits.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="request">The update request.</param>
        /// <param name="updatedByUserId">The user performing the action.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated organization limits response.</returns>
        Task<OrganizationLimitsResponse> UpdateOrganizationLimitsAsync(Guid organizationId, UpdateOrganizationLimitsRequest request, Guid updatedByUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the organization settings.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The organization settings response.</returns>
        Task<OrganizationSettingsResponse> GetOrganizationSettingsAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the organization settings.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="request">The update request.</param>
        /// <param name="updatedByUserId">The user performing the action.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated organization settings response.</returns>
        Task<OrganizationSettingsResponse> UpdateOrganizationSettingsAsync(Guid organizationId, UpdateOrganizationSettingsRequest request, Guid updatedByUserId, CancellationToken cancellationToken = default);
    }
}
