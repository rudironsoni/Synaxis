// <copyright file="InMemoryServiceDiscovery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Infrastructure.ServiceDiscovery;

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// In-memory implementation of service discovery for development and testing.
/// </summary>
public class InMemoryServiceDiscovery : IServiceDiscovery
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceInstance>> _services;
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryServiceDiscovery> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<IReadOnlyList<ServiceInstance>, Task>>> _watchers;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryServiceDiscovery"/> class.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    public InMemoryServiceDiscovery(IMemoryCache cache, ILogger<InMemoryServiceDiscovery> logger)
    {
        this._services = new ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceInstance>>(StringComparer.Ordinal);
        this._cache = cache;
        this._logger = logger;
        this._watchers = new ConcurrentDictionary<string, ConcurrentBag<Func<IReadOnlyList<ServiceInstance>, Task>>>(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public async Task RegisterAsync(string serviceName, ServiceInstance instance, CancellationToken cancellationToken = default)
    {
        var instances = this._services.GetOrAdd(serviceName, _ => new ConcurrentDictionary<string, ServiceInstance>(StringComparer.Ordinal));
        instances[instance.Id] = instance;

        this._logger.LogInformation(
            "Registered service instance {InstanceId} for {ServiceName} at {Address}",
            instance.Id,
            serviceName,
            instance.Address);

        // Notify watchers
        await this.NotifyWatchersAsync(serviceName).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeregisterAsync(string serviceName, string instanceId, CancellationToken cancellationToken = default)
    {
        if (this._services.TryGetValue(serviceName, out var instances))
        {
            instances.TryRemove(instanceId, out _);
            this._logger.LogInformation(
                "Deregistered service instance {InstanceId} for {ServiceName}",
                instanceId,
                serviceName);

            // Notify watchers
            await this.NotifyWatchersAsync(serviceName).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ServiceInstance>> DiscoverAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"sd:{serviceName}";
        if (this._cache.TryGetValue(cacheKey, out IReadOnlyList<ServiceInstance>? cachedInstances) && cachedInstances != null)
        {
            return Task.FromResult(cachedInstances);
        }

        var instances = this.GetHealthyInstances(serviceName);

        // Cache the result
        this._cache.Set(cacheKey, instances, TimeSpan.FromSeconds(30));

        return Task.FromResult<IReadOnlyList<ServiceInstance>>(instances);
    }

    /// <inheritdoc/>
    public Task<ServiceInstance?> GetInstanceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var instances = this.GetHealthyInstances(serviceName);

        if (instances.Count == 0)
        {
            return Task.FromResult<ServiceInstance?>(null);
        }

        // Simple round-robin load balancing
        var cacheKey = $"sd:rr:{serviceName}";
        var index = this._cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return 0;
        });

        var instance = instances[index % instances.Count];
        this._cache.Set(cacheKey, index + 1);

        return Task.FromResult<ServiceInstance?>(instance);
    }

    /// <inheritdoc/>
    public async Task UpdateHealthAsync(string serviceName, string instanceId, bool isHealthy, CancellationToken cancellationToken = default)
    {
        if (this._services.TryGetValue(serviceName, out var instances) &&
            instances.TryGetValue(instanceId, out var instance))
        {
            instance.Health = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
            instance.LastHealthCheck = DateTime.UtcNow;

            this._logger.LogDebug(
                "Updated health for {ServiceName}/{InstanceId} to {Health}",
                serviceName,
                instanceId,
                instance.Health);

            // Invalidate cache
            this._cache.Remove($"sd:{serviceName}");

            // Notify watchers
            await this.NotifyWatchersAsync(serviceName).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task WatchAsync(string serviceName, Func<IReadOnlyList<ServiceInstance>, Task> onChange, CancellationToken cancellationToken = default)
    {
        var watchers = this._watchers.GetOrAdd(serviceName, _ => new ConcurrentBag<Func<IReadOnlyList<ServiceInstance>, Task>>());
        watchers.Add(onChange);

        // Immediately invoke with current instances
        var instances = this.GetHealthyInstances(serviceName);
        await onChange(instances).ConfigureAwait(false);
    }

    private IReadOnlyList<ServiceInstance> GetHealthyInstances(string serviceName)
    {
        if (!this._services.TryGetValue(serviceName, out var instances))
        {
            return new List<ServiceInstance>();
        }

        return instances.Values
            .Where(i => i.Health == HealthStatus.Healthy)
            .ToList();
    }

    private async Task NotifyWatchersAsync(string serviceName)
    {
        if (this._watchers.TryGetValue(serviceName, out var watchers))
        {
            var instances = this.GetHealthyInstances(serviceName);
            var tasks = watchers.Select(async watcher =>
            {
                try
                {
                    await watcher(instances).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error notifying service discovery watcher");
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
