// <copyright file="ModelDiscoveryResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time notification when a new model is discovered.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="canonicalId">The canonical identifier of the model.</param>
    /// <param name="displayName">The display name of the model.</param>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="isAvailableToOrganization">Indicates whether the model is available to the organization.</param>
    public record ModelDiscoveryResult(
        Guid modelId,
        string canonicalId,
        string displayName,
        string providerName,
        bool isAvailableToOrganization);
}
