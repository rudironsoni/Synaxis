// <copyright file="ProviderStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Represents provider status.
    /// </summary>
    /// <param name="IsEnabled">Whether the provider is enabled.</param>
    /// <param name="IsHealthy">Whether the provider is healthy.</param>
    /// <param name="LastChecked">The last check timestamp.</param>
    public record ProviderStatus(bool IsEnabled, bool IsHealthy, DateTime? LastChecked);
}