// <copyright file="ModelDiscoveryResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time notification when a new model is discovered.
    /// </summary>
    /// <param name="ModelId">The model identifier.</param>
    /// <param name="CanonicalId">The canonical identifier of the model.</param>
    /// <param name="DisplayName">The display name of the model.</param>
    /// <param name="ProviderName">The name of the provider.</param>
    /// <param name="IsAvailableToOrganization">Indicates whether the model is available to the organization.</param>
    public record ModelDiscoveryResult(
        Guid ModelId,
        string CanonicalId,
        string DisplayName,
        string ProviderName,
        bool IsAvailableToOrganization);
}
