// <copyright file="IOrganizationUserContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Interfaces
{
    using System;

    /// <summary>
    /// Provides access to the current authenticated user's context.
    /// </summary>
    public interface IOrganizationUserContext
    {
        /// <summary>
        /// Gets the current user's ID.
        /// </summary>
        Guid UserId { get; }

        /// <summary>
        /// Gets the current user's organization ID.
        /// </summary>
        Guid? OrganizationId { get; }

        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}
