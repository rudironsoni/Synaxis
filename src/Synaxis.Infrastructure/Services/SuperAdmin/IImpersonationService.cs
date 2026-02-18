// <copyright file="IImpersonationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Threading.Tasks;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Service for impersonation operations.
    /// </summary>
    public interface IImpersonationService
    {
        /// <summary>
        /// Generates an impersonation token for a user.
        /// </summary>
        /// <param name="request">The impersonation request.</param>
        /// <returns>The impersonation token.</returns>
        Task<ImpersonationToken> GenerateImpersonationTokenAsync(ImpersonationRequest request);
    }
}
