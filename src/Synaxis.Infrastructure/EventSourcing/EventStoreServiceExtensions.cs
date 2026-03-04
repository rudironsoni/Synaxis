// <copyright file="EventStoreServiceExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.Infrastructure.EventSourcing.Aggregates;
using Synaxis.Infrastructure.EventSourcing.Persistence;
using Synaxis.Infrastructure.EventSourcing.Publishing;
using Synaxis.Infrastructure.EventSourcing.Serialization;
using Synaxis.Infrastructure.EventSourcing.Stores;

/// <summary>
/// Extension methods for registering event sourcing services.
/// </summary>
public static class EventStoreServiceExtensions
{
    /// <summary>
    /// Adds event sourcing infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="configureSerializer">Optional action to configure the serializer.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventSourcing(
        this IServiceCollection services,
        string connectionString,
        Action<System.Text.Json.JsonSerializerOptions>? configureSerializer = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        }

        // Configure JSON serializer options
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };
        configureSerializer?.Invoke(options);

        // Register services
        services.AddSingleton<IEventSerializer>(_ => new SystemTextJsonEventSerializer(options));
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        // Register DbContext
        services.AddDbContext<EventStoreDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(EventStoreDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "event_sourcing");
            });
        });

        // Register event store
        services.AddScoped<IEventStore, PostgreSqlEventStore>();
        services.AddScoped<ISnapshotStore, PostgreSqlSnapshotStore>();

        return services;
    }

    /// <summary>
    /// Adds an event-sourced aggregate repository to the service collection.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="snapshotThreshold">Number of events before creating a snapshot.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventSourcedAggregate<TAggregate>(
        this IServiceCollection services,
        int snapshotThreshold = 100)
        where TAggregate : EventSourcedAggregate, new()
    {
        services.AddScoped(sp =>
        {
            var eventStore = sp.GetRequiredService<IEventStore>();
            var serializer = sp.GetRequiredService<IEventSerializer>();
            var publisher = sp.GetService<IDomainEventPublisher>();
            var snapshotStore = sp.GetService<ISnapshotStore>();

            return new EventStoreRepository<TAggregate>(
                eventStore,
                serializer,
                publisher,
                snapshotStore,
                snapshotThreshold);
        });

        return services;
    }
}
