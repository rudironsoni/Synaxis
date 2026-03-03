// <copyright file="ProviderInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Represents provider information.
    /// </summary>
    /// <param name="Id">The provider ID.</param>
    /// <param name="Name">The provider name.</param>
    /// <param name="IsEnabled">Whether the provider is enabled.</param>
    /// <param name="InputCost">The input cost per token.</param>
    /// <param name="OutputCost">The output cost per token.</param>
    public record ProviderInfo(Guid Id, string Name, bool IsEnabled, decimal? InputCost, decimal? OutputCost);
}