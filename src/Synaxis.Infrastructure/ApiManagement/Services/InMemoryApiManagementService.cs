// <copyright file="InMemoryApiManagementService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Services;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.Infrastructure.ApiManagement.Abstractions;
using Synaxis.Infrastructure.ApiManagement.Configuration;
using Synaxis.Infrastructure.ApiManagement.Models;

/// <summary>
/// In-memory API Management service implementation for development and testing.
/// </summary>
public sealed class InMemoryApiManagementService : IApiManagementService
{
    private readonly ConcurrentDictionary<string, ApiKeyEntry> _keys = new();
    private readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimits = new();
    private readonly ILogger<InMemoryApiManagementService> _logger;
    private readonly InMemoryOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryApiManagementService"/> class.
    /// </summary>
    /// <param name="options">The API management options.</param>
    /// <param name="logger">The logger.</param>
    public InMemoryApiManagementService(
        IOptions<ApiManagementOptions> options,
        ILogger<InMemoryApiManagementService> logger)
    {
        this._options = options?.Value?.InMemory ?? new InMemoryOptions();
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Seed test keys if configured
        foreach (var testKey in this._options.TestKeys)
        {
            this._keys.TryAdd(testKey.Key, new ApiKeyEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                KeyValue = testKey.Key,
                DisplayName = testKey.Value,
                State = ApiKeyState.Active,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        this._logger.LogInformation("InMemoryApiManagementService initialized with {KeyCount} test keys", this._keys.Count);
    }

    /// <inheritdoc />
    public ApiManagementProvider Provider => ApiManagementProvider.InMemory;

    /// <inheritdoc />
    public Task<ApiKeyValidationResult> ValidateKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        this.SimulateLatency();

        if (!this._keys.TryGetValue(apiKey, out var entry))
        {
            this._logger.LogWarning("API key not found: {KeyPrefix}...", apiKey.Substring(0, Math.Min(8, apiKey.Length)));
            return Task.FromResult(this.CreateInvalidResult("Invalid API key"));
        }

        if (entry.State == ApiKeyState.Revoked)
        {
            this._logger.LogWarning("API key is revoked: {KeyId}", entry.Id);
            return Task.FromResult(this.CreateInvalidResult("API key has been revoked"));
        }

        if (entry.State == ApiKeyState.Expired ||
            (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTimeOffset.UtcNow))
        {
            this._logger.LogWarning("API key has expired: {KeyId}", entry.Id);
            return Task.FromResult(this.CreateInvalidResult("API key has expired"));
        }

        entry.LastUsedAt = DateTimeOffset.UtcNow;
        entry.UsageCount++;

        this._logger.LogDebug("API key validated successfully: {KeyId}", entry.Id);

        return Task.FromResult(new ApiKeyValidationResult
        {
            IsValid = true,
            KeyId = entry.Id,
            SubscriptionId = entry.Id,
            Scopes = entry.Scopes,
            Metadata = entry.Metadata,
        });
    }

    /// <inheritdoc />
    public Task<ApiKey> ProvisionKeyAsync(ProvisionKeyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DisplayName);

        this.SimulateLatency();

        var keyValue = this.GenerateApiKey();
        var keyId = Guid.NewGuid().ToString("N");

        var entry = new ApiKeyEntry
        {
            Id = keyId,
            KeyValue = keyValue,
            DisplayName = request.DisplayName,
            Scopes = request.Scope != null
                ? new List<string> { request.Scope }.Concat(request.Scopes).Distinct().ToList()
                : request.Scopes.ToList(),
            ExpiresAt = request.ExpiresAt,
            Metadata = new Dictionary<string, string>(request.Metadata),
            State = ApiKeyState.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        this._keys.TryAdd(keyValue, entry);

        // Initialize rate limit entry
        this._rateLimits.TryAdd(keyId, new RateLimitEntry
        {
            KeyId = keyId,
            WindowStart = DateTimeOffset.UtcNow,
            RequestCount = 0,
        });

        this._logger.LogInformation("Provisioned new API key: {KeyId} for {DisplayName}", keyId, request.DisplayName);

        return Task.FromResult(new ApiKey
        {
            Id = keyId,
            KeyValue = keyValue,
            DisplayName = request.DisplayName,
            SubscriptionId = keyId,
            State = ApiKeyState.Active,
            Scopes = entry.Scopes,
            ExpiresAt = request.ExpiresAt,
            Metadata = new Dictionary<string, string>(request.Metadata),
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }

    /// <inheritdoc />
    public Task<bool> RevokeKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        this.SimulateLatency();

        var entry = this._keys.Values.FirstOrDefault(k => k.Id == keyId);
        if (entry == null)
        {
            this._logger.LogWarning("API key not found for revocation: {KeyId}", keyId);
            return Task.FromResult(false);
        }

        entry.State = ApiKeyState.Revoked;
        entry.RevokedAt = DateTimeOffset.UtcNow;

        this._logger.LogInformation("Revoked API key: {KeyId}", keyId);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ConfigureRateLimitAsync(string keyId, RateLimitConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentNullException.ThrowIfNull(config);

        this.SimulateLatency();

        var entry = this._rateLimits.AddOrUpdate(
            keyId,
            _ => new RateLimitEntry
            {
                KeyId = keyId,
                Config = config,
                WindowStart = DateTimeOffset.UtcNow,
                RequestCount = 0,
            },
            (_, existing) => new RateLimitEntry
            {
                KeyId = keyId,
                Config = config,
                WindowStart = DateTimeOffset.UtcNow,
                RequestCount = existing.RequestCount,
            });

        this._logger.LogInformation("Configured rate limit for key {KeyId}: {RequestsPerWindow}/{WindowSeconds}s",
            keyId, config.RequestsPerWindow, config.WindowSeconds);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<RateLimitStatus> GetRateLimitStatusAsync(string keyId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        this.SimulateLatency();

        if (!this._rateLimits.TryGetValue(keyId, out var entry) || entry.Config == null)
        {
            return Task.FromResult(new RateLimitStatus
            {
                IsRateLimited = false,
                RemainingRequests = -1,
                TotalRequestsAllowed = -1,
                RequestsMade = 0,
                WindowResetTime = DateTimeOffset.UtcNow.AddMinutes(1),
            });
        }

        var now = DateTimeOffset.UtcNow;
        var windowEnd = entry.WindowStart.AddSeconds(entry.Config.WindowSeconds);

        if (now > windowEnd)
        {
            // Window has reset
            entry.WindowStart = now;
            entry.RequestCount = 0;
            windowEnd = now.AddSeconds(entry.Config.WindowSeconds);
        }

        var remaining = Math.Max(0, entry.Config.RequestsPerWindow - entry.RequestCount);
        var isRateLimited = entry.RequestCount >= entry.Config.RequestsPerWindow;

        return Task.FromResult(new RateLimitStatus
        {
            IsRateLimited = isRateLimited,
            RemainingRequests = remaining,
            TotalRequestsAllowed = entry.Config.RequestsPerWindow,
            RequestsMade = entry.RequestCount,
            WindowResetTime = windowEnd,
            RetryAfter = isRateLimited ? windowEnd - now : null,
        });
    }

    /// <inheritdoc />
    public Task<UsageReport> GetUsageReportAsync(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        IDictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default)
    {
        this.SimulateLatency();

        var relevantKeys = this._keys.Values.Where(k =>
            k.CreatedAt <= endTime &&
            (k.RevokedAt == null || k.RevokedAt >= startTime));

        var endpointBreakdown = relevantKeys
            .SelectMany(k => k.Scopes.Select(s => new { Scope = s, Key = k }))
            .GroupBy(x => x.Scope)
            .Select(g => new EndpointUsage
            {
                Endpoint = g.Key,
                Method = "*",
                CallCount = g.Sum(x => x.Key.UsageCount),
                SuccessCount = g.Sum(x => x.Key.UsageCount),
                ErrorCount = 0,
                AverageResponseTimeMs = 0,
            })
            .ToList();

        var subscriptionBreakdown = relevantKeys
            .Select(k => new SubscriptionUsage
            {
                SubscriptionId = k.Id,
                DisplayName = k.DisplayName,
                CallCount = k.UsageCount,
                DataTransferBytes = 0,
                RateLimitHits = 0,
            })
            .ToList();

        var totalCalls = relevantKeys.Sum(k => k.UsageCount);

        this._logger.LogInformation("Generated usage report: {TotalCalls} total calls from {StartTime} to {EndTime}",
            totalCalls, startTime, endTime);

        return Task.FromResult(new UsageReport
        {
            StartTime = startTime,
            EndTime = endTime,
            TotalCalls = totalCalls,
            SuccessfulCalls = totalCalls,
            FailedCalls = 0,
            BlockedCalls = 0,
            TotalDataTransferBytes = 0,
            AverageResponseTimeMs = 0,
            MaxResponseTimeMs = 0,
            MinResponseTimeMs = 0,
            EndpointBreakdown = endpointBreakdown,
            SubscriptionBreakdown = subscriptionBreakdown,
            HourlyBreakdown = new List<HourlyUsage>(),
            StatusCodeDistribution = new Dictionary<int, long>(),
        });
    }

    private void SimulateLatency()
    {
        if (this._options.SimulatedLatencyMs > 0)
        {
            Thread.Sleep(this._options.SimulatedLatencyMs);
        }
    }

    private string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return "synaxis_test_" + Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "").Substring(0, 32);
    }

    private ApiKeyValidationResult CreateInvalidResult(string errorMessage)
    {
        return new ApiKeyValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            Scopes = new List<string>(),
            Metadata = new Dictionary<string, string>(),
        };
    }

    private sealed class ApiKeyEntry
    {
        public string Id { get; set; } = string.Empty;
        public string KeyValue { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ApiKeyState State { get; set; } = ApiKeyState.Active;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? LastUsedAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public List<string> Scopes { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
        public long UsageCount { get; set; }
    }

    private sealed class RateLimitEntry
    {
        public string KeyId { get; set; } = string.Empty;
        public RateLimitConfig? Config { get; set; }
        public DateTimeOffset WindowStart { get; set; }
        public int RequestCount { get; set; }
    }
}
