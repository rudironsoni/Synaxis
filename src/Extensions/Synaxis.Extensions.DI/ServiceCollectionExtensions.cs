// <copyright file="ServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Extensions.DI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synaxis.Abstractions.Cloud;
using Synaxis.Abstractions.Time;
using Synaxis.Contracts.V1.Messages;
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
    /// Adds Synaxis event sourcing services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method uses explicit registration pattern to ensure clear dependency graph.
    /// No auto-discovery or reflection-based registration is used.
    /// </remarks>
    public static IServiceCollection AddSynaxisEventSourcing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Event store
        services.TryAddSingleton<IEventStore, InMemoryEventStore>();

        // Snapshot strategies
        services.TryAddSingleton<ISnapshotStrategy, NoSnapshotStrategy>();

        return services;
    }

    /// <summary>
    /// Adds Synaxis encryption services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Configures encryption providers with explicit registration pattern.
    /// </remarks>
    public static IServiceCollection AddSynaxisEncryption(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Encryption service
        services.TryAddSingleton<IEncryptionService, EncryptionService>();

        return services;
    }

    /// <summary>
    /// Adds Synaxis messaging services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Configures message handlers and publishers with explicit registration pattern.
    /// </remarks>
    public static IServiceCollection AddSynaxisMessaging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Time provider
        services.TryAddSingleton<ITimeProvider, SystemTimeProvider>();

        // Outbox
        services.TryAddSingleton<IOutbox, SqlOutbox>();
        services.AddOptions<OutboxOptions>();

        return services;
    }

    /// <summary>
    /// Adds Synaxis Azure provider services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Configures Azure-specific services with explicit registration pattern.
    /// </remarks>
    public static IServiceCollection AddSynaxisAzureProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Azure client
        services.TryAddSingleton<AzureClient>();

        // Azure Key Vault
        services.TryAddSingleton<IKeyVault, AzureKeyVault>();

        // Azure Service Bus
        services.TryAddSingleton<IMessageBus, AzureServiceBus>();

        // Azure Cosmos Event Store
        services.TryAddSingleton<AzureCosmosEventStore>();

        // Azure SQL Event Store
        services.TryAddSingleton<AzureSqlEventStore>();

        // Azure Snapshot Store
        services.TryAddSingleton<SnapshotStore>();

        return services;
    }
}
