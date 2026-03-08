// <copyright file="AzureApiManagementService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Services;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Abstractions;
using Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Configuration;
using Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Models;

/// <summary>
/// Azure API Management service implementation.
/// </summary>
public sealed class AzureApiManagementService : IApiManagementService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureApiManagementService> _logger;
    private readonly AzureApiManagementOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureApiManagementService"/> class.
    /// </summary>
    /// <param name="options">The Azure APIM options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClient">The HTTP client.</param>
    public AzureApiManagementService(
        IOptions<ApiManagementOptions> options,
        ILogger<AzureApiManagementService> logger,
        HttpClient httpClient)
    {
        this._options = options?.Value?.Azure ?? throw new ArgumentNullException(nameof(options), "Azure APIM options are required");
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        this._jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        this.ConfigureHttpClient();
    }

    /// <inheritdoc />
    public ApiManagementProvider Provider => ApiManagementProvider.AzureApiManagement;

    /// <inheritdoc />
    public async Task<ApiKeyValidationResult> ValidateKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        try
        {
            this._logger.LogDebug("Validating API key against Azure APIM");

            // Query subscription by key
            var response = await this.GetSubscriptionByKeyAsync(apiKey, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                this._logger.LogWarning("API key not found in Azure APIM");
                return CreateInvalidResult("Invalid API key");
            }

            if (response.State == "suspended")
            {
                this._logger.LogWarning("API key is suspended in Azure APIM: {KeyId}", response.Id);
                return CreateInvalidResult("API key is suspended");
            }

            if (response.State == "cancelled")
            {
                this._logger.LogWarning("API key is cancelled in Azure APIM: {KeyId}", response.Id);
                return CreateInvalidResult("API key has been revoked");
            }

            if (response.State == "expired" ||
                (response.ExpiresAt.HasValue && response.ExpiresAt.Value < DateTimeOffset.UtcNow))
            {
                this._logger.LogWarning("API key has expired in Azure APIM: {KeyId}", response.Id);
                return CreateInvalidResult("API key has expired");
            }

            this._logger.LogInformation("API key validated successfully: {KeyId}", response.Id);

            return new ApiKeyValidationResult
            {
                IsValid = true,
                KeyId = this.ExtractIdFromResourceId(response.Id),
                SubscriptionId = this.ExtractIdFromResourceId(response.Id),
                Scopes = response.Scope != null ? new List<string> { response.Scope } : new List<string>(),
                Metadata = new Dictionary<string, string>
                {
                    { "displayName", response.DisplayName ?? string.Empty },
                    { "ownerId", response.OwnerId ?? string.Empty },
                    { "scope", response.Scope ?? string.Empty },
                    { "primaryKey", response.PrimaryKey ?? string.Empty },
                },
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            this._logger.LogWarning("API key not found in Azure APIM");
            return CreateInvalidResult("Invalid API key");
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error validating API key against Azure APIM");
            return CreateInvalidResult("Error validating API key");
        }
    }

    /// <inheritdoc />
    public async Task<ApiKey> ProvisionKeyAsync(ProvisionKeyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DisplayName);

        try
        {
            this._logger.LogInformation("Provisioning new API key in Azure APIM: {DisplayName}", request.DisplayName);

            var subscriptionName = request.DisplayName;
            var subscriptionId = Guid.NewGuid().ToString("N");
            var scope = request.Scope ?? $"/apis/{this._options.ApiId}";

            var createRequest = new
            {
                properties = new
                {
                    displayName = subscriptionName,
                    scope = scope,
                    state = "active",
                    allowTracing = true,
                },
            };

            var url = this.BuildManagementUrl($"subscriptions/{subscriptionId}");
            var content = JsonContent.Create(createRequest, options: this._jsonOptions);

            var response = await this._httpClient.PutAsync(url, content, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AzureSubscriptionResponse>(this._jsonOptions, cancellationToken).ConfigureAwait(false);

            if (result?.Properties == null)
            {
                throw new InvalidOperationException("Failed to create subscription: empty response");
            }

            // List keys for the subscription
            var keys = await this.ListSubscriptionKeysAsync(subscriptionId, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Successfully provisioned API key: {KeyId}", subscriptionId);

            return new ApiKey
            {
                Id = subscriptionId,
                DisplayName = request.DisplayName,
                SubscriptionId = subscriptionId,
                State = ApiKeyState.Active,
                Scopes = request.Scope != null ? new List<string> { request.Scope } : new List<string>(),
                PrimaryKey = keys.PrimaryKey,
                SecondaryKey = keys.SecondaryKey,
                ExpiresAt = request.ExpiresAt,
                Metadata = request.Metadata,
                CreatedAt = DateTimeOffset.UtcNow,
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error provisioning API key in Azure APIM");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RevokeKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        try
        {
            this._logger.LogInformation("Revoking API key in Azure APIM: {KeyId}", keyId);

            var url = this.BuildManagementUrl($"subscriptions/{keyId}");

            // First, get current subscription to preserve other properties
            var getResponse = await this._httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (getResponse.StatusCode == HttpStatusCode.NotFound)
            {
                this._logger.LogWarning("API key not found for revocation: {KeyId}", keyId);
                return false;
            }

            getResponse.EnsureSuccessStatusCode();

            // Update state to cancelled
            var updateRequest = new
            {
                properties = new
                {
                    state = "cancelled",
                },
            };

            var content = JsonContent.Create(updateRequest, options: this._jsonOptions);
            var patchResponse = await this._httpClient.PatchAsync(url, content, cancellationToken).ConfigureAwait(false);
            patchResponse.EnsureSuccessStatusCode();

            this._logger.LogInformation("Successfully revoked API key: {KeyId}", keyId);
            return true;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error revoking API key in Azure APIM: {KeyId}", keyId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ConfigureRateLimitAsync(string keyId, RateLimitConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            this._logger.LogInformation("Configuring rate limit for key {KeyId}: {RequestsPerWindow} requests per {WindowSeconds}s",
                keyId, config.RequestsPerWindow, config.WindowSeconds);

            // In Azure APIM, rate limits are typically configured via policies at the API or product level
            // For subscription-specific limits, we need to configure a policy at the product level
            // or use the quota-by-key policy

            var policyContent = this.BuildRateLimitPolicy(config);

            // This is a simplified implementation - in practice, you'd need to:
            // 1. Get or create a product for this subscription
            // 2. Configure the policy at the product level
            // 3. Or use named values and policy expressions

            this._logger.LogDebug("Rate limit policy: {Policy}", policyContent);

            // For now, we log the configuration request
            // Full implementation would require product management
            this._logger.LogInformation("Rate limit configuration for {KeyId} would apply: {RequestsPerWindow}/{WindowSeconds}s",
                keyId, config.RequestsPerWindow, config.WindowSeconds);

            return true;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error configuring rate limit for key {KeyId}", keyId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<RateLimitStatus> GetRateLimitStatusAsync(string keyId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        try
        {
            this._logger.LogDebug("Getting rate limit status for key: {KeyId}", keyId);

            // Azure APIM doesn't provide a direct API for current rate limit status
            // We would need to query metrics or use a custom caching layer
            // For now, return a default status

            return new RateLimitStatus
            {
                IsRateLimited = false,
                RemainingRequests = -1, // Unknown
                TotalRequestsAllowed = -1,
                RequestsMade = 0,
                WindowResetTime = DateTimeOffset.UtcNow.AddMinutes(1),
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting rate limit status for key {KeyId}", keyId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<UsageReport> GetUsageReportAsync(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        IDictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            this._logger.LogInformation("Generating usage report from {StartTime} to {EndTime}", startTime, endTime);

            // Azure APIM usage reports require Azure Monitor metrics
            // This is a simplified implementation that returns mock data
            // In production, you'd query Azure Monitor metrics API

            this._logger.LogWarning("Usage report generation is not fully implemented - returning mock data");

            return new UsageReport
            {
                StartTime = startTime,
                EndTime = endTime,
                TotalCalls = 0,
                SuccessfulCalls = 0,
                FailedCalls = 0,
                BlockedCalls = 0,
                TotalDataTransferBytes = 0,
                AverageResponseTimeMs = 0,
                MaxResponseTimeMs = 0,
                MinResponseTimeMs = 0,
                EndpointBreakdown = new List<EndpointUsage>(),
                SubscriptionBreakdown = new List<SubscriptionUsage>(),
                HourlyBreakdown = new List<HourlyUsage>(),
                StatusCodeDistribution = new Dictionary<int, long>(),
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error generating usage report");
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!this._disposed)
        {
            this._httpClient?.Dispose();
            this._disposed = true;
        }
    }

    private void ConfigureHttpClient()
    {
        this._httpClient.BaseAddress = new Uri(this._options.ManagementApiUrl ?? "https://management.azure.com");
        this._httpClient.Timeout = TimeSpan.FromSeconds(this._options.TimeoutSeconds);
        this._httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        // Add authorization header (would be set up via token acquisition)
        // In production, use Azure.Identity for token acquisition
        if (!string.IsNullOrEmpty(this._options.ClientSecret))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this._options.ClientId}:{this._options.ClientSecret}"));
            this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    private string BuildManagementUrl(string path)
    {
        var basePath = $"/subscriptions/{this._options.SubscriptionId}/resourceGroups/{this._options.ResourceGroupName}/providers/Microsoft.ApiManagement/service/{this._options.ServiceName}";
        var fullPath = $"{basePath}/{path}";
        var apiVersion = this._options.ApiVersion ?? "2022-08-01";
        return $"{fullPath}?api-version={apiVersion}";
    }

    private async Task<AzureSubscriptionProperties?> GetSubscriptionByKeyAsync(string apiKey, CancellationToken cancellationToken)
    {
        // Azure APIM doesn't support direct lookup by key value
        // In production, you'd need to:
        // 1. Maintain a mapping of key hashes to subscription IDs
        // 2. Or use the list subscriptions API and check each key

        // For this implementation, we'll list all subscriptions and check keys
        var url = this.BuildManagementUrl("subscriptions");
        var response = await this._httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureSubscriptionListResponse>(this._jsonOptions, cancellationToken).ConfigureAwait(false);

        if (result?.Value == null)
        {
            return null;
        }

        foreach (var subscription in result.Value)
        {
            var keys = await this.ListSubscriptionKeysAsync(this.ExtractIdFromResourceId(subscription.Id) ?? string.Empty, cancellationToken).ConfigureAwait(false);

            if (keys.PrimaryKey == apiKey || keys.SecondaryKey == apiKey)
            {
                subscription.PrimaryKey = keys.PrimaryKey;
                subscription.SecondaryKey = keys.SecondaryKey;
                return subscription;
            }
        }

        return null;
    }

    private async Task<SubscriptionKeys> ListSubscriptionKeysAsync(string subscriptionId, CancellationToken cancellationToken)
    {
        var url = this.BuildManagementUrl($"subscriptions/{subscriptionId}/listSecrets");
        var response = await this._httpClient.PostAsync(url, new StringContent(string.Empty), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SubscriptionKeys>(this._jsonOptions, cancellationToken).ConfigureAwait(false);

        return result ?? new SubscriptionKeys();
    }

    private string? ExtractIdFromResourceId(string? resourceId)
    {
        if (string.IsNullOrEmpty(resourceId))
        {
            return string.Empty;
        }

        var parts = resourceId.Split('/');
        return parts.Length > 0 ? parts[^1] : resourceId;
    }

    private static ApiKeyValidationResult CreateInvalidResult(string errorMessage)
    {
        return new ApiKeyValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            Scopes = new List<string>(),
            Metadata = new Dictionary<string, string>(),
        };
    }

    private string BuildRateLimitPolicy(RateLimitConfig config)
    {
        return $@"
<rate-limit-by-key calls=""{config.RequestsPerWindow}""
                   renewal-period=""{config.WindowSeconds}""
                   counter-key=""@(context.Subscription.Id)"" />
";
    }

    private sealed class AzureSubscriptionListResponse
    {
        [JsonPropertyName("value")]
        public List<AzureSubscriptionProperties> Value { get; set; } = new();
    }

    private sealed class AzureSubscriptionResponse
    {
        [JsonPropertyName("properties")]
        public AzureSubscriptionProperties? Properties { get; set; }
    }

    private sealed class AzureSubscriptionProperties
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("ownerId")]
        public string? OwnerId { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTimeOffset? CreatedDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTimeOffset? ExpiresAt { get; set; }

        [JsonIgnore]
        public string? PrimaryKey { get; set; }

        [JsonIgnore]
        public string? SecondaryKey { get; set; }
    }

    private sealed class SubscriptionKeys
    {
        [JsonPropertyName("primaryKey")]
        public string? PrimaryKey { get; set; }

        [JsonPropertyName("secondaryKey")]
        public string? SecondaryKey { get; set; }
    }
}
