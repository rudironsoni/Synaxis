// <copyright file="IProviderTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Tool for managing provider configurations.
    /// </summary>
    public interface IProviderTool
    {
        /// <summary>
        /// Updates a provider configuration setting.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="key">The configuration key.</param>
        /// <param name="value">The configuration value.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>True if successful, false otherwise.</returns>
        Task<bool> UpdateProviderConfigAsync(Guid organizationId, Guid providerId, string key, object value, CancellationToken ct = default);

        /// <summary>
        /// Gets the status of a provider.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The provider status.</returns>
        Task<ProviderStatus> GetProviderStatusAsync(Guid organizationId, Guid providerId, CancellationToken ct = default);

        /// <summary>
        /// Gets all providers for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>List of provider information.</returns>
        Task<List<ProviderInfo>> GetAllProvidersAsync(Guid organizationId, CancellationToken ct = default);
    }

    /// <summary>
    /// Represents provider status information.
    /// </summary>
    /// <param name="IsEnabled">Whether the provider is enabled.</param>
    /// <param name="IsHealthy">Whether the provider is healthy.</param>
    /// <param name="LastChecked">The last check timestamp.</param>
    public record ProviderStatus(bool IsEnabled, bool IsHealthy, DateTime? LastChecked);

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
