// <copyright file="ProviderAccountStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Provider account status options.
    /// </summary>
    public enum ProviderAccountStatus
    {
        /// <summary>Provider account is active.</summary>
        Active,

        /// <summary>Provider account is in cooldown.</summary>
        Cooldown,

        /// <summary>Provider account is disabled.</summary>
        Disabled,
    }
}
