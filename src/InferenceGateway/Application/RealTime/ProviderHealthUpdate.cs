// <copyright file="ProviderHealthUpdate.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time update for provider health status changes.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="isHealthy">Indicates whether the provider is healthy.</param>
    /// <param name="healthScore">The health score of the provider.</param>
    /// <param name="averageLatencyMs">The average latency in milliseconds.</param>
    /// <param name="checkedAt">The date and time when the health was checked.</param>
    public record ProviderHealthUpdate(
        Guid providerId,
        string providerName,
        bool isHealthy,
        decimal healthScore,
        int averageLatencyMs,
        DateTime checkedAt);
}
