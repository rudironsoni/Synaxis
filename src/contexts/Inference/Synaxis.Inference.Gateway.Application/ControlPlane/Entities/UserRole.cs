// <copyright file="UserRole.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// User role options.
    /// </summary>
    public enum UserRole
    {
        /// <summary>Owner role.</summary>
        Owner,

        /// <summary>Admin role.</summary>
        Admin,

        /// <summary>Developer role.</summary>
        Developer,

        /// <summary>Read-only role.</summary>
        Readonly,
    }
}
