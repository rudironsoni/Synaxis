// <copyright file="RedisHealthStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Routing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;
    using Synaxis.InferenceGateway.Application.Routing;

    public class RedisHealthStore : IHealthStore
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisHealthStore> _logger;

        public RedisHealthStore(IConnectionMultiplexer redis, ILogger<RedisHealthStore> logger)
        {
            this._redis = redis;
            this._logger = logger;
        }

        public async Task<bool> IsHealthyAsync(string providerKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _redis.GetDatabase();
                // If the penalty key exists, the provider is unhealthy
                return !await db.KeyExistsAsync($"health:{providerKey}:penalty");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check health for provider '{ProviderKey}'. Failing open (healthy).", providerKey);
                return true;
            }
        }

        public async Task MarkFailureAsync(string providerKey, TimeSpan cooldown, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync($"health:{providerKey}:penalty", "1", cooldown);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark failure for provider '{ProviderKey}'.", providerKey);
            }
        }

        public Task MarkSuccessAsync(string providerKey, CancellationToken cancellationToken = default)
        {
            _ = providerKey;
            // Optional: Could track success stats or reduce penalty level
            return Task.CompletedTask;
        }
    }
}
