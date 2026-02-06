// <copyright file="CostOptimizationResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time notification when cost optimization is applied.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="fromProvider">The name of the provider being switched from.</param>
    /// <param name="toProvider">The name of the provider being switched to.</param>
    /// <param name="reason">The reason for the optimization.</param>
    /// <param name="savingsPer1MTokens">The savings per 1 million tokens.</param>
    /// <param name="appliedAt">The date and time when the optimization was applied.</param>
    public record CostOptimizationResult(
        Guid organizationId,
        string fromProvider,
        string toProvider,
        string reason,
        decimal savingsPer1MTokens,
        DateTime appliedAt);
}
