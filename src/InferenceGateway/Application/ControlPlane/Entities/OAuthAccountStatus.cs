// <copyright file="OAuthAccountStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// OAuth account status options.
    /// </summary>
    public enum OAuthAccountStatus
    {
        /// <summary>OAuth account is active.</summary>
        Active,

        /// <summary>OAuth account is revoked.</summary>
        Revoked,
    }
}
