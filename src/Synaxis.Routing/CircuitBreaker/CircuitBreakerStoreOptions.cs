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
