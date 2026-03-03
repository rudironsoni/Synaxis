// <copyright file="ProviderRequestStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Status of a provider request in the approval workflow.
    /// </summary>
    public enum ProviderRequestStatus
    {
        /// <summary>Initial state - awaiting admin review.</summary>
        Pending,

        /// <summary>Approved by admin, awaiting health check.</summary>
        PendingHealthCheck,

        /// <summary>Admin approved, health check passed.</summary>
        Approved,

        /// <summary>Admin rejected.</summary>
        Rejected,

        /// <summary>Actively routing requests.</summary>
        Active,

        /// <summary>Deactivated by admin.</summary>
        Deactivated,
    }
}
