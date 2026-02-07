// <copyright file="ApiKeyStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// API key status options.
    /// </summary>
    public enum ApiKeyStatus
    {
        /// <summary>API key is active.</summary>
        Active,

        /// <summary>API key is revoked.</summary>
        Revoked,
    }
}
