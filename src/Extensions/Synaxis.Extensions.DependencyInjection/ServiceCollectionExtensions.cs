// <copyright file="ServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Extensions.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Synaxis.Abstractions.Cloud;
using Synaxis.Configuration.Options;
using Synaxis.Extensions.DependencyInjection.HealthChecks;
using Synaxis.Infrastructure.Encryption;
using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Infrastructure.Messaging;
using Synaxis.Providers.Azure;
using Synaxis.Providers.Azure.EventStores;

/// <summary>
/// Extension methods for configuring Synaxis services with explicit registration pattern.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Synaxis configuration services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration to bind options from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CloudProviderOptions>()
            .Bind(configuration.GetSection("Cloud"))
            .ValidateDataAnnotations();

        services.AddOptions<AzureOptions>()
            .Bind(configuration.GetSection("Cloud:Azure"))
            .ValidateDataAnnotations();

        services.AddOptions<AwsOptions>()
            .Bind(configuration.GetSection("Cloud:Aws"))
            .ValidateDataAnnotations();

        services.AddOptions<GcpOptions>()
            .Bind(configuration.GetSection("Cloud:Gcp"))
            .ValidateDataAnnotations();

        services.AddOptions<OnPremiseOptions>()
            .Bind(configuration.GetSection("Cloud:OnPremise"))
            .ValidateDataAnnotations();

        services.AddOptions<EventStoreOptions>()
            .Bind(configuration.GetSection("EventStore"))
            .ValidateDataAnnotations();

        services.AddOptions<KeyVaultOptions>()
            .Bind(configuration.GetSection("KeyVault"))
            .ValidateDataAnnotations();

        services.AddOptions<MessageBusOptions>()
            .Bind(configuration.GetSection("MessageBus"))
            .ValidateDataAnnotations();

        return services;
    }

    /// <summary>
    /// Adds Synaxis event sourcing services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisEventSourcing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IEventStore, InMemoryEventStore>();
        services.TryAddSingleton<ISnapshotStrategy, NoSnapshotStrategy>();

        return services;
    }

    /// <summary>
    /// Adds Synaxis encryption services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisEncryption(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IEncryptionService, EncryptionService>();

        return services;
    }

    /// <summary>
    /// Adds Synaxis messaging services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisMessaging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IOutbox, SqlOutbox>();

        return services;
    }

    /// <summary>
    /// Adds Synaxis Azure provider services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisAzureProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<AzureClient>();
        services.TryAddSingleton<IKeyVault, AzureKeyVault>();
        services.TryAddSingleton<IMessageBus, AzureServiceBus>();
        services.TryAddSingleton<AzureCosmosEventStore>();
        services.TryAddSingleton<AzureSqlEventStore>();
        services.TryAddSingleton<SnapshotStore>();

        return services;
    }

    /// <summary>
    /// Adds all Synaxis services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration to bind options from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisAll(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSynaxisConfiguration(configuration);
        services.AddSynaxisEventSourcing();
        services.AddSynaxisEncryption();
        services.AddSynaxisMessaging();
        services.AddSynaxisAzureProvider();

        services.AddHealthChecks()
            .AddCheck<ConfigurationHealthCheck>("configuration")
            .AddCheck<ServiceHealthCheck>("services");

        return services;
    }
}
