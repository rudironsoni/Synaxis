// <copyright file="KongApiManagementService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Services;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.Infrastructure.ApiManagement.Abstractions;
using Synaxis.Infrastructure.ApiManagement.Configuration;
using Synaxis.Infrastructure.ApiManagement.Models;

/// <summary>
/// Kong API Gateway management service implementation.
/// </summary>
public sealed class KongApiManagementService : IApiManagementService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KongApiManagementService> _logger;
    private readonly KongOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KongApiManagementService"/> class.
    /// </summary>
    /// <param name="options">The API management options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClient">The HTTP client.</param>
    public KongApiManagementService(
        IOptions<ApiManagementOptions> options,
        ILogger<KongApiManagementService> logger,
        HttpClient httpClient)
    {
        this._options = options?.Value?.Kong ?? throw new ArgumentNullException(nameof(options), "Kong options are required");
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
    public ApiManagementProvider Provider => ApiManagementProvider.Kong;

    /// <inheritdoc />
    public async Task<ApiKeyValidationResult> ValidateKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        try
        {
            this._logger.LogDebug("Validating API key against Kong");

            // Query the key-auth credential by key value
            var url = $"/key-auths/{Uri.EscapeDataString(apiKey)}";
            var response = await this._httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                this._logger.LogWarning("API key not found in Kong");
                return CreateInvalidResult("Invalid API key");
            }

            response.EnsureSuccessStatusCode();

            var credential = await response.Content.ReadFromJsonAsync<KongKeyAuthCredential>(this._jsonOptions, cancellationToken).ConfigureAwait(false);

            if (credential?.ConsumerId == null)
            {
                this._logger.LogWarning("Invalid API key response from Kong");
                return CreateInvalidResult("Invalid API key");
            }

            // Get consumer details
            var consumer = await this.GetConsumerAsync(credential.ConsumerId, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("API key validated successfully: Consumer={ConsumerId}", credential.ConsumerId);

            return new ApiKeyValidationResult
            {
                IsValid = true,
                KeyId = credential.Id,
                SubscriptionId = credential.ConsumerId,
                Scopes = consumer?.Tags ?? new List<string>(),
                Metadata = new Dictionary<string, string>
                {
                    { "consumerId", credential.ConsumerId },
                    { "createdAt", credential.CreatedAt.ToString() },
                    { "consumerUsername", consumer?.Username ?? string.Empty },
                },
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            this._logger.LogWarning("API key not found in Kong");
            return CreateInvalidResult("Invalid API key");
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error validating API key against Kong");
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
            this._logger.LogInformation("Provisioning new API key in Kong: {DisplayName}", request.DisplayName);

            // Create or get consumer
            var consumer = await this.CreateConsumerAsync(request.DisplayName, request.Metadata, cancellationToken).ConfigureAwait(false);

            // Create key-auth credential for the consumer
            var keyValue = this.GenerateApiKey();
            var credential = await this.CreateKeyAuthCredentialAsync(consumer.Id, keyValue, cancellationToken).ConfigureAwait(false);

            // Apply rate limiting plugin if configured
            if (this._options.ConsumerPattern != null)
            {
                await ConfigureRateLimitingForConsumerAsync(consumer.Id, cancellationToken).ConfigureAwait(false);
            }

            this._logger.LogInformation("Successfully provisioned API key: {KeyId}", credential.Id);

            return new ApiKey
            {
                Id = credential.Id,
                KeyValue = keyValue,
                DisplayName = request.DisplayName,
                SubscriptionId = consumer.Id,
                State = ApiKeyState.Active,
                Scopes = request.Scope != null ? new List<string> { request.Scope } : new List<string>(),
                Metadata = request.Metadata,
                CreatedAt = DateTimeOffset.UtcNow,
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error provisioning API key in Kong");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RevokeKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        try
        {
            this._logger.LogInformation("Revoking API key in Kong: {KeyId}", keyId);

            var url = $"/consumers/{Uri.EscapeDataString(keyId)}";
            var response = await this._httpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                this._logger.LogWarning("Consumer not found for revocation: {KeyId}", keyId);
                return false;
            }

            response.EnsureSuccessStatusCode();

            this._logger.LogInformation("Successfully revoked API key: {KeyId}", keyId);
            return true;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error revoking API key in Kong: {KeyId}", keyId);
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
            this._logger.LogInformation("Configuring rate limit for consumer {KeyId}: {RequestsPerWindow} requests per {WindowSeconds}s",
                keyId, config.RequestsPerWindow, config.WindowSeconds);

            // Create rate limiting plugin for the consumer
            var pluginConfig = new
            {
                name = "rate-limiting",
                consumer = new { id = keyId },
                config = new
                {
                    minute = config.RequestsPerWindow,
                    policy = "local",
                    fault_tolerant = true,
                    hide_client_headers = false,
                    redis_timeout = 2000,
                },
            };

            var url = "/plugins";
            var content = JsonContent.Create(pluginConfig, options: this._jsonOptions);
            var response = await this._httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                this._logger.LogError("Failed to configure rate limiting: {Error}", errorBody);
                return false;
            }

            this._logger.LogInformation("Successfully configured rate limiting for consumer {KeyId}", keyId);
            return true;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error configuring rate limit for consumer {KeyId}", keyId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<RateLimitStatus> GetRateLimitStatusAsync(string keyId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        try
        {
            this._logger.LogDebug("Getting rate limit status for consumer: {KeyId}", keyId);

            // Kong doesn't provide a direct API for current rate limit status
            // We would need to use a custom plugin or cache layer
            // For now, return a default status

            return new RateLimitStatus
            {
                IsRateLimited = false,
                RemainingRequests = -1,
                TotalRequestsAllowed = -1,
                RequestsMade = 0,
                WindowResetTime = DateTimeOffset.UtcNow.AddMinutes(1),
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting rate limit status for consumer {KeyId}", keyId);
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

            // Kong doesn't have built-in analytics API in open source
            // Enterprise version has Vitals API
            // This is a simplified implementation

            this._logger.LogWarning("Usage report generation requires Kong Enterprise - returning mock data");

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
        var baseUrl = this._options.AdminApiUrl;
        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += "/";
        }

        this._httpClient.BaseAddress = new Uri(baseUrl);
        this._httpClient.Timeout = TimeSpan.FromSeconds(this._options.TimeoutSeconds);
        this._httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        // Add authentication
        if (!string.IsNullOrEmpty(this._options.AdminApiKey))
        {
            this._httpClient.DefaultRequestHeaders.Add("apikey", this._options.AdminApiKey);
        }
        else if (!string.IsNullOrEmpty(this._options.Username) && !string.IsNullOrEmpty(this._options.Password))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this._options.Username}:{this._options.Password}"));
            this._httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        }
    }

    private async Task<KongConsumer> CreateConsumerAsync(
        string username,
        IDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        var consumerData = new
        {
            username = username.ToLowerInvariant().Replace(" ", "-"),
            tags = metadata.TryGetValue("tags", out var tags) ? tags.Split(',') : Array.Empty<string>(),
            custom_id = metadata.TryGetValue("customId", out var customId) ? customId : null,
        };

        var content = JsonContent.Create(consumerData, options: this._jsonOptions);
        var response = await this._httpClient.PostAsync("/consumers", content, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            // Consumer already exists, fetch it
            return await GetConsumerByUsernameAsync(consumerData.username, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Failed to create or retrieve consumer: {consumerData.username}");
        }

        response.EnsureSuccessStatusCode();

        var consumer = await response.Content.ReadFromJsonAsync<KongConsumer>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
        return consumer ?? throw new InvalidOperationException("Empty response when creating consumer");
    }

    private async Task<KongConsumer?> GetConsumerAsync(string consumerId, CancellationToken cancellationToken)
    {
        var response = await this._httpClient.GetAsync($"/consumers/{Uri.EscapeDataString(consumerId)}", cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KongConsumer>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task<KongConsumer?> GetConsumerByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var response = await this._httpClient.GetAsync($"/consumers/{Uri.EscapeDataString(username)}", cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KongConsumer>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task<KongKeyAuthCredential> CreateKeyAuthCredentialAsync(
        string consumerId,
        string keyValue,
        CancellationToken cancellationToken)
    {
        var credentialData = new
        {
            key = keyValue,
        };

        var content = JsonContent.Create(credentialData, options: this._jsonOptions);
        var response = await this._httpClient.PostAsync($"/consumers/{Uri.EscapeDataString(consumerId)}/key-auth", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var credential = await response.Content.ReadFromJsonAsync<KongKeyAuthCredential>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
        return credential ?? throw new InvalidOperationException("Empty response when creating key-auth credential");
    }

    private async Task ConfigureRateLimitingForConsumerAsync(string consumerId, CancellationToken cancellationToken)
    {
        var pluginConfig = new
        {
            name = "rate-limiting",
            consumer = new { id = consumerId },
            config = new
            {
                minute = 100,
                hour = 1000,
                policy = "local",
            },
        };

        var content = JsonContent.Create(pluginConfig, options: this._jsonOptions);
        await _httpClient.PostAsync("/plugins", content, cancellationToken).ConfigureAwait(false);
    }

    private string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
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

    private sealed class KongConsumer
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("custom_id")]
        public string? CustomId { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }
    }

    private sealed class KongKeyAuthCredential
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("consumer")]
        public KongConsumerReference? Consumer { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonIgnore]
        public string ConsumerId => Consumer?.Id ?? string.Empty;
    }

    private sealed class KongConsumerReference
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
