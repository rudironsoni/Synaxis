// <copyright file="CircuitBreakerStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

namespace Synaxis.Routing.CircuitBreaker;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

/// <summary>
/// Stores circuit breaker instances with in-memory storage and optional Redis backup.
/// </summary>
public sealed class CircuitBreakerStore : IDisposable
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> _inMemoryStore;
    private readonly CircuitBreakerStoreOptions _options;
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly IDatabase? _redisDatabase;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerStore"/> class.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public CircuitBreakerStore(CircuitBreakerStoreOptions? options = null)
    {
        this._options = options ?? new CircuitBreakerStoreOptions();
        this._inMemoryStore = new ConcurrentDictionary<string, CircuitBreaker>(StringComparer.Ordinal);
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        if (this._options.EnableRedisBackup && !string.IsNullOrEmpty(this._options.RedisConnectionString))
        {
            try
            {
                this._redisConnection = ConnectionMultiplexer.Connect(this._options.RedisConnectionString);
                this._redisDatabase = this._redisConnection.GetDatabase();
            }
            catch (Exception ex)
            {
                // Log warning but continue with in-memory only
                Console.WriteLine($"Warning: Failed to connect to Redis: {ex.Message}. Using in-memory storage only.");
            }
        }
    }

    /// <summary>
    /// Gets or creates a circuit breaker for the specified provider.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="options">The circuit breaker options. If null, default options will be used.</param>
    /// <returns>The circuit breaker instance.</returns>
    public async Task<CircuitBreaker> GetOrCreateAsync(string providerName, CircuitBreakerOptions? options = null)
    {
        if (string.IsNullOrEmpty(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));
        }

        // Try to get from in-memory store first
        if (this._inMemoryStore.TryGetValue(providerName, out var existingBreaker))
        {
            return existingBreaker;
        }

        // Try to load from Redis if available
        if (this._redisDatabase != null)
        {
            try
            {
                var redisKey = this.GetRedisKey(providerName);
                var redisValue = await this._redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

                if (!redisValue.IsNullOrEmpty)
                {
                    var state = JsonSerializer.Deserialize<CircuitBreakerState>(redisValue.ToString(), this._jsonSerializerOptions);
                    if (state != null)
                    {
                        var breaker = CircuitBreakerStore.CreateCircuitBreaker(providerName, options ?? new CircuitBreakerOptions());
                        CircuitBreakerStore.RestoreCircuitBreakerState(breaker, state);
                        this._inMemoryStore.TryAdd(providerName, breaker);
                        return breaker;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log warning but continue with creating a new circuit breaker
                Console.WriteLine($"Warning: Failed to load circuit breaker from Redis: {ex.Message}.");
            }
        }

        // Create a new circuit breaker
        var newBreaker = CircuitBreakerStore.CreateCircuitBreaker(providerName, options ?? new CircuitBreakerOptions());
        this._inMemoryStore.TryAdd(providerName, newBreaker);
        return newBreaker;
    }

    /// <summary>
    /// Gets a circuit breaker for the specified provider without creating one if it doesn't exist.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <returns>The circuit breaker instance, or null if it doesn't exist.</returns>
    public CircuitBreaker? Get(string providerName)
    {
        if (string.IsNullOrEmpty(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));
        }

        this._inMemoryStore.TryGetValue(providerName, out var breaker);
        return breaker;
    }

    /// <summary>
    /// Gets all circuit breakers in the store.
    /// </summary>
    /// <returns>A dictionary of provider names to circuit breakers.</returns>
    public IReadOnlyDictionary<string, CircuitBreaker> GetAll()
    {
        return this._inMemoryStore.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// Saves the state of a circuit breaker to Redis.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="breaker">The circuit breaker to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveAsync(string providerName, CircuitBreaker breaker)
    {
        if (string.IsNullOrEmpty(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));
        }

        if (breaker == null)
        {
            throw new ArgumentNullException(nameof(breaker));
        }

        if (this._redisDatabase == null)
        {
            return;
        }

        try
        {
            var state = this.ExtractCircuitBreakerState(breaker);
            var redisKey = this.GetRedisKey(providerName);
            var redisValue = JsonSerializer.Serialize(state, this._jsonSerializerOptions);

            await this._redisDatabase.StringSetAsync(
                redisKey,
                redisValue,
                TimeSpan.FromSeconds(this._options.RedisExpirationSeconds)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log warning but don't throw
            Console.WriteLine($"Warning: Failed to save circuit breaker to Redis: {ex.Message}.");
        }
    }

    /// <summary>
    /// Removes a circuit breaker from the store.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveAsync(string providerName)
    {
        if (string.IsNullOrEmpty(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));
        }

        this._inMemoryStore.TryRemove(providerName, out _);

        if (this._redisDatabase != null)
        {
            try
            {
                var redisKey = this.GetRedisKey(providerName);
                await this._redisDatabase.KeyDeleteAsync(redisKey).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log warning but don't throw
                Console.WriteLine($"Warning: Failed to delete circuit breaker from Redis: {ex.Message}.");
            }
        }
    }

    /// <summary>
    /// Clears all circuit breakers from the store.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClearAsync()
    {
        this._inMemoryStore.Clear();

        if (this._redisDatabase != null)
        {
            try
            {
                var server = this._redisConnection?.GetServer(this._redisConnection.GetEndPoints()[0]);
                if (server != null)
                {
                    var keys = server.Keys(pattern: $"{this._options.RedisKeyPrefix}*").ToArray();
                    if (keys.Length > 0)
                    {
                        await this._redisDatabase.KeyDeleteAsync(keys).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log warning but don't throw
                Console.WriteLine($"Warning: Failed to clear circuit breakers from Redis: {ex.Message}.");
            }
        }
    }

    /// <summary>
    /// Gets the health status of all circuit breakers.
    /// </summary>
    /// <returns>A dictionary of provider names to their circuit breaker states.</returns>
    public IReadOnlyDictionary<string, CircuitBreakerState> GetHealthStatus()
    {
        return this._inMemoryStore.ToDictionary(
            kvp => kvp.Key,
            kvp => this.ExtractCircuitBreakerState(kvp.Value),
            StringComparer.Ordinal);
    }

    private string GetRedisKey(string providerName)
    {
        return $"{this._options.RedisKeyPrefix}{providerName}";
    }

    private static CircuitBreaker CreateCircuitBreaker(string name, CircuitBreakerOptions options)
    {
        return new CircuitBreaker(name, options);
    }

    private CircuitBreakerState ExtractCircuitBreakerState(CircuitBreaker breaker)
    {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        // Use reflection to access private fields
        // Justification: Required to serialize circuit breaker state for Redis persistence
        // The CircuitBreaker class is in the same assembly and this is a controlled access pattern
        var lastFailureTimeField = breaker.GetType().GetField("_lastFailureTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var openedAtField = breaker.GetType().GetField("_openedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var consecutiveSuccessesField = breaker.GetType().GetField("_consecutiveSuccessesInHalfOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return new CircuitBreakerState
        {
            State = breaker.State,
            FailureCount = breaker.FailureCount,
            SuccessCount = breaker.SuccessCount,
            TotalRequests = breaker.TotalRequests,
            LastFailureTime = lastFailureTimeField?.GetValue(breaker) as DateTime?,
            OpenedAt = openedAtField?.GetValue(breaker) as DateTime?,
            ConsecutiveSuccessesInHalfOpen = consecutiveSuccessesField?.GetValue(breaker) as int? ?? 0,
        };
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
    }

    private static void RestoreCircuitBreakerState(CircuitBreaker breaker, CircuitBreakerState state)
    {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        // Use reflection to set private fields
        // Justification: Required to deserialize circuit breaker state from Redis persistence
        // The CircuitBreaker class is in the same assembly and this is a controlled access pattern
        var failureCountField = breaker.GetType().GetField("_failureCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var successCountField = breaker.GetType().GetField("_successCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var totalRequestsField = breaker.GetType().GetField("_totalRequests", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lastFailureTimeField = breaker.GetType().GetField("_lastFailureTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var openedAtField = breaker.GetType().GetField("_openedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var consecutiveSuccessesField = breaker.GetType().GetField("_consecutiveSuccessesInHalfOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        failureCountField?.SetValue(breaker, state.FailureCount);
        successCountField?.SetValue(breaker, state.SuccessCount);
        totalRequestsField?.SetValue(breaker, state.TotalRequests);
        lastFailureTimeField?.SetValue(breaker, state.LastFailureTime ?? default(DateTime));
        openedAtField?.SetValue(breaker, state.OpenedAt ?? default(DateTime));
        consecutiveSuccessesField?.SetValue(breaker, state.ConsecutiveSuccessesInHalfOpen);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
    }

    /// <summary>
    /// Disposes the Redis connection if it exists.
    /// </summary>
    public void Dispose()
    {
        this._redisConnection?.Dispose();
    }
}
