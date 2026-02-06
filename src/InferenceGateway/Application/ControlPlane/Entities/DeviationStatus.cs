// <copyright file="DeviationStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Deviation status options.
    /// </summary>
    public enum DeviationStatus
    {
        /// <summary>Deviation is open.</summary>
        Open,

        /// <summary>Deviation is mitigated.</summary>
        Mitigated,

        /// <summary>Deviation is closed.</summary>
        Closed,
    }
}
