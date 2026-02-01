using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Application.Configuration;

namespace Synaxis.InferenceGateway.Infrastructure.Routing;

public class RedisQuotaTracker : IQuotaTracker
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisQuotaTracker> _logger;
    private readonly SynaxisConfiguration _config;

    // Lua script for atomic rate limiting check and increment
    // Returns 1 if allowed, 0 if limit exceeded
    private const string CheckQuotaLuaScript = @"
local rpmKey = KEYS[1]
local tpmKey = KEYS[2]
local maxRpm = tonumber(ARGV[1])
local maxTpm = tonumber(ARGV[2])

-- Get current values
local currentRpm = tonumber(redis.call('GET', rpmKey)) or 0
local currentTpm = tonumber(redis.call('GET', tpmKey)) or 0

-- Check RPM limit
if maxRpm and currentRpm >= maxRpm then
    return 0  -- Exceeded RPM limit
end

-- Check TPM limit
if maxTpm and currentTpm >= maxTpm then
    return 0  -- Exceeded TPM limit
end

-- Increment RPM counter (TPM is incremented separately in RecordUsageAsync)
redis.call('INCR', rpmKey)
redis.call('EXPIRE', rpmKey, 60)

-- Return 1 for allowed
return 1
";

    public RedisQuotaTracker(
        IConnectionMultiplexer redis,
        ILogger<RedisQuotaTracker> logger,
        IOptions<SynaxisConfiguration> config)
    {
        _redis = redis;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<bool> CheckQuotaAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.Providers.TryGetValue(providerKey, out var providerConfig))
            {
                _logger.LogDebug("Provider '{ProviderKey}' not found in configuration, allowing request.", providerKey);
                return true;
            }

            var db = _redis.GetDatabase();
            var now = DateTimeOffset.UtcNow;
            var currentMinute = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Offset).ToUnixTimeSeconds();

            var rpmKey = $"ratelimit:{providerKey}:rpm:{currentMinute}";
            var tpmKey = $"ratelimit:{providerKey}:tpm:{currentMinute}";

            var maxRpm = providerConfig.RateLimitRPM;
            var maxTpm = providerConfig.RateLimitTPM;

            // If no rate limits configured, allow request
            if (!maxRpm.HasValue && !maxTpm.HasValue)
            {
                return true;
            }

            // Execute Lua script atomically to check and increment
            var result = await db.ScriptEvaluateAsync(
                CheckQuotaLuaScript,
                new RedisKey[] { rpmKey, tpmKey },
                new RedisValue[] { maxRpm ?? -1, maxTpm ?? -1 }
            );

            var allowed = (long)result == 1;

            if (!allowed)
            {
                // Log which limit was exceeded (we need to check again for logging purposes)
                var currentRpm = await db.StringGetAsync(rpmKey);
                var currentTpm = await db.StringGetAsync(tpmKey);
                var rpmValue = currentRpm.HasValue ? (long)currentRpm : 0;
                var tpmValue = currentTpm.HasValue ? (long)currentTpm : 0;

                if (maxRpm.HasValue && rpmValue >= maxRpm.Value)
                {
                    _logger.LogWarning("Provider '{ProviderKey}' exceeded RPM limit: {CurrentRpm}/{MaxRpm}", providerKey, rpmValue, maxRpm.Value);
                }
                else if (maxTpm.HasValue && tpmValue >= maxTpm.Value)
                {
                    _logger.LogWarning("Provider '{ProviderKey}' exceeded TPM limit: {CurrentTpm}/{MaxTpm}", providerKey, tpmValue, maxTpm.Value);
                }
            }

            return allowed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check quota for provider '{ProviderKey}'. Allowing request as fallback.", providerKey);
            return true;
        }
    }

    public Task<bool> IsHealthyAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        return CheckQuotaAsync(providerKey, cancellationToken);
    }

    public async Task RecordUsageAsync(string providerKey, long inputTokens, long outputTokens, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var now = DateTimeOffset.UtcNow;
            var currentMinute = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Offset).ToUnixTimeSeconds();

            var tpmKey = $"ratelimit:{providerKey}:tpm:{currentMinute}";
            var totalTokens = inputTokens + outputTokens;

            await db.StringIncrementAsync(tpmKey, totalTokens);
            await db.KeyExpireAsync(tpmKey, TimeSpan.FromMinutes(1));

            await db.StringIncrementAsync($"quota:{providerKey}:tokens", totalTokens);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record usage for provider '{ProviderKey}'.", providerKey);
        }
    }
}
