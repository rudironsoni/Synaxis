// <copyright file="IApiManagementService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Abstractions;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service abstraction for API Management platform integration.
/// Supports multiple providers (Azure APIM, Kong) for API key management,
/// rate limiting, and usage analytics.
/// </summary>
public interface IApiManagementService
{
    /// <summary>
    /// Validates an API key against the external API Management platform.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result containing key metadata if valid.</returns>
    Task<ApiKeyValidationResult> ValidateKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provisions a new API key in the external API Management platform.
    /// </summary>
    /// <param name="request">The provisioning request containing key details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The provisioned API key details.</returns>
    Task<Models.ApiKey> ProvisionKeyAsync(ProvisionKeyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an existing API key.
    /// </summary>
    /// <param name="keyId">The unique identifier of the key to revoke.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if revocation was successful.</returns>
    Task<bool> RevokeKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures rate limiting for an API key or subscription.
    /// </summary>
    /// <param name="keyId">The unique identifier of the key.</param>
    /// <param name="config">The rate limit configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if configuration was successful.</returns>
    Task<bool> ConfigureRateLimitAsync(string keyId, Models.RateLimitConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current rate limit status for an API key.
    /// </summary>
    /// <param name="keyId">The unique identifier of the key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current rate limit status.</returns>
    Task<Models.RateLimitStatus> GetRateLimitStatusAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves usage analytics for the specified time range.
    /// </summary>
    /// <param name="startTime">The start time of the reporting period.</param>
    /// <param name="endTime">The end time of the reporting period.</param>
    /// <param name="filters">Optional filters for the report (e.g., specific key or subscription).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The usage report containing analytics data.</returns>
    Task<Models.UsageReport> GetUsageReportAsync(
        System.DateTimeOffset startTime,
        System.DateTimeOffset endTime,
        IDictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the provider type for this API management service.
    /// </summary>
    ApiManagementProvider Provider { get; }
}
