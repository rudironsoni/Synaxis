// <copyright file="IOrganizationLimitService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Threading.Tasks;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Service for organization limit operations.
    /// </summary>
    public interface IOrganizationLimitService
    {
        /// <summary>
        /// Modifies organization limits.
        /// </summary>
        /// <param name="request">The limit modification request.</param>
        /// <returns>True if successful.</returns>
        Task<bool> ModifyOrganizationLimitsAsync(LimitModificationRequest request);
    }
}
