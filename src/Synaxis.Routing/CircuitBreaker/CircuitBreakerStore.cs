using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Synaxis.Routing.CircuitBreaker;

/// <summary>
/// Configuration options for the circuit breaker store.
/// </summary>
public class CircuitBreakerStoreOptions
{
    /// <summary>
    /// Gets or sets the Redis connection string.
    /// If null or empty, only in-memory storage will be used.
    /// </summary>
    public string RedisConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the Redis key prefix for circuit breaker data.
    /// Default is "circuitbreaker:".
    /// </summary>
    public string RedisKeyPrefix { get; set; } = "circuitbreaker:";

    /// <summary>
    /// Gets or sets the expiration time (in seconds) for Redis entries.
    /// Default is 3600 (1 hour).
    /// </summary>
    public int RedisExpirationSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets whether to enable Redis backup.
    /// Default is true.
    /// </summary>
    public bool EnableRedisBackup { get; set; } = true;
}

/// <summary>
/// Represents the state of a circuit breaker for serialization.
/// </summary>
public class CircuitBreakerState
{
    public CircuitState State { get; set; }
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }
    public int TotalRequests { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public DateTime? OpenedAt { get; set; }
    public int ConsecutiveSuccessesInHalfOpen { get; set; }
}

/// <summary>
/// Stores circuit breaker instances with in-memory storage and optional Redis backup.
/// </summary>
public class CircuitBreakerStore
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
    public CircuitBreakerStore(CircuitBreakerStoreOptions options = null)
    {
        _options = options ?? new CircuitBreakerStoreOptions();
        _inMemoryStore = new ConcurrentDictionary<string, CircuitBreaker>();
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        if (_options.EnableRedisBackup && !string.IsNullOrEmpty(_options.RedisConnectionString))
        {
            try
            {
                _redisConnection = ConnectionMultiplexer.Connect(_options.RedisConnectionString);
                _redisDatabase = _redisConnection.GetDatabase();
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
        if (_inMemoryStore.TryGetValue(providerName, out var existingBreaker))
        {
            return existingBreaker;
        }

        // Try to load from Redis if available
        if (_redisDatabase != null)
        {
            try
            {
                var redisKey = GetRedisKey(providerName);
                var redisValue = await _redisDatabase.StringGetAsync(redisKey);

                if (!redisValue.IsNullOrEmpty)
                {
                    var state = JsonSerializer.Deserialize<CircuitBreakerState>(redisValue.ToString(), _jsonSerializerOptions);
                    if (state != null)
                    {
                        var breaker = CreateCircuitBreaker(providerName, options ?? new CircuitBreakerOptions());
                        RestoreCircuitBreakerState(breaker, state);
                        _inMemoryStore.TryAdd(providerName, breaker);
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
        var newBreaker = CreateCircuitBreaker(providerName, options ?? new CircuitBreakerOptions());
        _inMemoryStore.TryAdd(providerName, newBreaker);
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

        _inMemoryStore.TryGetValue(providerName, out var breaker);
        return breaker;
    }

    /// <summary>
    /// Gets all circuit breakers in the store.
    /// </summary>
    /// <returns>A dictionary of provider names to circuit breakers.</returns>
    public IReadOnlyDictionary<string, CircuitBreaker> GetAll()
    {
        return _inMemoryStore.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Saves the state of a circuit breaker to Redis.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="breaker">The circuit breaker to save.</param>
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

        if (_redisDatabase == null)
        {
            return;
        }

        try
        {
            var state = ExtractCircuitBreakerState(breaker);
            var redisKey = GetRedisKey(providerName);
            var redisValue = JsonSerializer.Serialize(state, _jsonSerializerOptions);

            await _redisDatabase.StringSetAsync(
                redisKey,
                redisValue,
                TimeSpan.FromSeconds(_options.RedisExpirationSeconds));
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
    public async Task RemoveAsync(string providerName)
    {
        if (string.IsNullOrEmpty(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));
        }

        _inMemoryStore.TryRemove(providerName, out _);

        if (_redisDatabase != null)
        {
            try
            {
                var redisKey = GetRedisKey(providerName);
                await _redisDatabase.KeyDeleteAsync(redisKey);
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
    public async Task ClearAsync()
    {
        _inMemoryStore.Clear();

        if (_redisDatabase != null)
        {
            try
            {
                var server = _redisConnection?.GetServer(_redisConnection.GetEndPoints().First());
                if (server != null)
                {
                    var keys = server.Keys(pattern: $"{_options.RedisKeyPrefix}*").ToArray();
                    if (keys.Length > 0)
                    {
                        await _redisDatabase.KeyDeleteAsync(keys);
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
    public Dictionary<string, CircuitBreakerState> GetHealthStatus()
    {
        return _inMemoryStore.ToDictionary(
            kvp => kvp.Key,
            kvp => ExtractCircuitBreakerState(kvp.Value));
    }

    private string GetRedisKey(string providerName)
    {
        return $"{_options.RedisKeyPrefix}{providerName}";
    }

    private CircuitBreaker CreateCircuitBreaker(string name, CircuitBreakerOptions options)
    {
        return new CircuitBreaker(name, options);
    }

    private CircuitBreakerState ExtractCircuitBreakerState(CircuitBreaker breaker)
    {
        // Use reflection to access private fields
        var stateField = breaker.GetType().GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var failureCountField = breaker.GetType().GetField("_failureCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var successCountField = breaker.GetType().GetField("_successCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var totalRequestsField = breaker.GetType().GetField("_totalRequests", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
            ConsecutiveSuccessesInHalfOpen = consecutiveSuccessesField?.GetValue(breaker) as int? ?? 0
        };
    }

    private void RestoreCircuitBreakerState(CircuitBreaker breaker, CircuitBreakerState state)
    {
        // Use reflection to set private fields
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
    }

    /// <summary>
    /// Disposes the Redis connection if it exists.
    /// </summary>
    public void Dispose()
    {
        _redisConnection?.Dispose();
    }
}
