// <copyright file="CostOptimizationResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time notification when cost optimization is applied.
    /// </summary>
    /// <param name="OrganizationId">The organization identifier.</param>
    /// <param name="FromProvider">The name of the provider being switched from.</param>
    /// <param name="ToProvider">The name of the provider being switched to.</param>
    /// <param name="Reason">The reason for the optimization.</param>
    /// <param name="SavingsPer1MTokens">The savings per 1 million tokens.</param>
    /// <param name="AppliedAt">The date and time when the optimization was applied.</param>
    public record CostOptimizationResult(
        Guid OrganizationId,
        string FromProvider,
        string ToProvider,
        string Reason,
        decimal SavingsPer1MTokens,
        DateTime AppliedAt);
}
