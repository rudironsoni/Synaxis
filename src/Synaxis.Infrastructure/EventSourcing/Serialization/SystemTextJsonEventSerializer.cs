// <copyright file="SystemTextJsonEventSerializer.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing.Serialization;

using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Event serializer implementation using System.Text.Json with polymorphic support.
/// </summary>
public sealed class SystemTextJsonEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _options;
    private readonly ConcurrentDictionary<string, Type?> _typeCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonEventSerializer"/> class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options.</param>
    public SystemTextJsonEventSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? CreateDefaultOptions();
    }

    /// <inheritdoc/>
    public string Serialize<TEvent>(TEvent @event)
        where TEvent : notnull
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        return JsonSerializer.Serialize(@event, _options);
    }

    /// <inheritdoc/>
    public object Deserialize(string json, Type eventType)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON cannot be null or empty", nameof(json));
        }

        if (eventType is null)
        {
            throw new ArgumentNullException(nameof(eventType));
        }

        var result = JsonSerializer.Deserialize(json, eventType, _options);

        if (result is null)
        {
            throw new EventDeserializationException($"Failed to deserialize event of type {eventType.FullName}");
        }

        return result;
    }

    /// <inheritdoc/>
    public Type? ResolveType(string typeName)
    {
        return _typeCache.GetOrAdd(typeName, name =>
        {
            // Try exact type resolution first
            var type = Type.GetType(name);

            if (type is not null)
            {
                return type;
            }

            // Search in all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(name);
                if (type is not null)
                {
                    return type;
                }
            }

            // Try with just the type name (no namespace) if it contains a dot
            if (name.Contains('.', StringComparison.Ordinal))
            {
                var shortName = name[(name.LastIndexOf('.') + 1)..];
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in assembly.GetTypes())
                    {
                        if (t.Name == shortName)
                        {
                            return t;
                        }
                    }
                }
            }

            return null;
        });
    }

    /// <summary>
    /// Creates default JSON serializer options with polymorphic support.
    /// </summary>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };

        // Add polymorphic serialization support
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        return options;
    }
}
