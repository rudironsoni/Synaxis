// <copyright file="EventSerializer.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Provides JSON serialization and deserialization for domain events with type information.
/// </summary>
public class EventSerializer
{
    private readonly JsonSerializerSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSerializer"/> class.
    /// </summary>
    public EventSerializer()
    {
        this._settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };
    }

    /// <summary>
    /// Serializes a domain event to JSON.
    /// </summary>
    /// <param name="event">The domain event to serialize.</param>
    /// <returns>A JSON string representation of the event.</returns>
    public string Serialize(IDomainEvent @event)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        return JsonConvert.SerializeObject(@event, this._settings);
    }

    /// <summary>
    /// Deserializes a JSON string to a domain event.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized domain event.</returns>
    public IDomainEvent Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON string cannot be null or whitespace.", nameof(json));
        }

        return JsonConvert.DeserializeObject<IDomainEvent>(json, this._settings)
            ?? throw new InvalidOperationException("Failed to deserialize event from JSON.");
    }

    /// <summary>
    /// Deserializes a JSON string to a domain event of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of domain event to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized domain event.</returns>
    public T Deserialize<T>(string json)
        where T : IDomainEvent
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON string cannot be null or whitespace.", nameof(json));
        }

        return JsonConvert.DeserializeObject<T>(json, this._settings)
            ?? throw new InvalidOperationException($"Failed to deserialize event of type {typeof(T).Name} from JSON.");
    }

    /// <summary>
    /// Serializes a collection of domain events to JSON.
    /// </summary>
    /// <param name="events">The collection of domain events to serialize.</param>
    /// <returns>A JSON string representation of the events.</returns>
    public string SerializeMany(IEnumerable<IDomainEvent> events)
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        return JsonConvert.SerializeObject(events, this._settings);
    }

    /// <summary>
    /// Deserializes a JSON string to a collection of domain events.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A collection of deserialized domain events.</returns>
    public IReadOnlyList<IDomainEvent> DeserializeMany(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON string cannot be null or whitespace.", nameof(json));
        }

        var events = JsonConvert.DeserializeObject<IReadOnlyList<IDomainEvent>>(json, this._settings)
            ?? throw new InvalidOperationException("Failed to deserialize events from JSON.");

        return events;
    }
}
