// <copyright file="ServiceHealthCheck.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Extensions.DependencyInjection.HealthChecks;

using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.Encryption;
using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Infrastructure.Messaging;

/// <summary>
/// Health check for validating service dependencies.
/// </summary>
public class ServiceHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public ServiceHealthCheck(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Runs the health check, returning the status of service dependencies.
    /// </summary>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
    /// <returns>A <see cref="Task"/> that completes when the health check has finished, yielding the health check result.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        _ = cancellationToken;

        return this.CheckHealthInternalAsync();
    }

    private Task<HealthCheckResult> CheckHealthInternalAsync()
    {
        var data = new Dictionary<string, object>(StringComparer.Ordinal);
        var degraded = false;
        var exception = default(Exception);

        try
        {
            this.CheckService<IEventStore>("EventStore", data, ref degraded);
            this.CheckService<IEncryptionService>("EncryptionService", data, ref degraded);
            this.CheckService<IMessageBus>("MessageBus", data, ref degraded);
            this.CheckService<IOutbox>("Outbox", data, ref degraded);
            this.CheckService<IKeyVault>("KeyVault", data, ref degraded);
        }
        catch (Exception ex)
        {
            degraded = true;
            exception = ex;
        }

        if (exception != null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Service dependency check failed.",
                exception,
                new ReadOnlyDictionary<string, object>(data)));
        }

        if (degraded)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Some service dependencies are not registered.",
                data: new ReadOnlyDictionary<string, object>(data)));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "All service dependencies are available.",
            new ReadOnlyDictionary<string, object>(data)));
    }

    private void CheckService<TService>(string name, Dictionary<string, object> data, ref bool degraded)
        where TService : class
    {
        var service = this._serviceProvider.GetService<TService>();
        data[name] = service != null ? "Available" : "Not registered";
        if (service == null)
        {
            degraded = true;
        }
    }
}
