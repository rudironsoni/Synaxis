using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Synaxis.InferenceGateway.Application.Routing;

namespace Synaxis.InferenceGateway.Infrastructure.Routing;

public class RedisQuotaTracker : IQuotaTracker
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisQuotaTracker> _logger;

    public RedisQuotaTracker(IConnectionMultiplexer redis, ILogger<RedisQuotaTracker> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public Task<bool> CheckQuotaAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        // Placeholder: Always true for now. 
        // Future: Check against configured limits per provider/tier.
        return Task.FromResult(true);
    }

    public async Task RecordUsageAsync(string providerKey, long inputTokens, long outputTokens, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.StringIncrementAsync($"quota:{providerKey}:tokens", inputTokens + outputTokens);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record usage for provider '{ProviderKey}'.", providerKey);
        }
    }
}
