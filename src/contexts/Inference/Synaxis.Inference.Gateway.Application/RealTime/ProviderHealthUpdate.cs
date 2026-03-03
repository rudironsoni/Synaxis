// <copyright file="ProviderHealthUpdate.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time update for provider health status changes.
    /// </summary>
    /// <param name="ProviderId">The provider identifier.</param>
    /// <param name="ProviderName">The name of the provider.</param>
    /// <param name="IsHealthy">Indicates whether the provider is healthy.</param>
    /// <param name="HealthScore">The health score of the provider.</param>
    /// <param name="AverageLatencyMs">The average latency in milliseconds.</param>
    /// <param name="CheckedAt">The date and time when the health was checked.</param>
    public record ProviderHealthUpdate(
        Guid ProviderId,
        string ProviderName,
        bool IsHealthy,
        decimal HealthScore,
        int AverageLatencyMs,
        DateTime CheckedAt);
}
