using Synaxis.InferenceGateway.Application.Configuration.Models;

namespace Synaxis.InferenceGateway.Application.Configuration;

/// <summary>
/// Service for resolving configuration settings hierarchically.
/// Resolution order: User → Group → Organization → Global
/// </summary>
public interface IConfigurationResolver
{
    /// <summary>
    /// Gets a configuration setting with hierarchical resolution.
    /// Searches in order: User settings, Group settings, Organization settings, Global defaults.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="userId">The optional user ID for user-level settings.</param>
    /// <param name="organizationId">The optional organization ID for organization-level settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configuration setting with its value and source.</returns>
    Task<ConfigurationSetting<T>> GetSettingAsync<T>(
        string key,
        Guid? userId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective rate limits for a user or organization.
    /// Resolution order: User membership → Group → Organization settings → Global defaults.
    /// </summary>
    /// <param name="userId">The optional user ID.</param>
    /// <param name="organizationId">The optional organization ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective rate limit configuration.</returns>
    Task<RateLimitConfiguration> GetRateLimitsAsync(
        Guid? userId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective cost per 1M tokens for a model in an organization.
    /// Resolution order: OrganizationModel → OrganizationProvider → Provider defaults.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="modelId">The model ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective cost configuration.</returns>
    Task<CostConfiguration> GetEffectiveCostPer1MTokensAsync(
        Guid organizationId,
        Guid providerId,
        Guid modelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if auto-optimization is enabled for a user or organization.
    /// Resolution order: User membership → Group → Organization settings → Global default (true).
    /// </summary>
    /// <param name="userId">The optional user ID.</param>
    /// <param name="organizationId">The optional organization ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if auto-optimization should be enabled, otherwise false.</returns>
    Task<bool> ShouldAutoOptimizeAsync(
        Guid? userId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
}
