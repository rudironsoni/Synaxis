// <copyright file="ISuperAdminAccessValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System.Threading.Tasks;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Service for validating super admin access.
    /// </summary>
    public interface ISuperAdminAccessValidator
    {
        /// <summary>
        /// Validates super admin access.
        /// </summary>
        /// <param name="context">The access context.</param>
        /// <returns>The validation result.</returns>
        Task<SuperAdminAccessValidation> ValidateAccessAsync(SuperAdminAccessContext context);

        /// <summary>
        /// Checks if an IP address is allowed.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if allowed.</returns>
        bool IsIpAllowed(string ipAddress);
    }
}
