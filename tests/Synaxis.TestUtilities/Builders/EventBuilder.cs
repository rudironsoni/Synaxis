// <copyright file="EventBuilder.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Builders;

using System.Globalization;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Builder for creating domain events in tests.
/// </summary>
/// <typeparam name="TEvent">The type of event to build.</typeparam>
public class EventBuilder<TEvent>
    where TEvent : class, IDomainEvent, new()
{
    private readonly TEvent _event;
    private readonly Dictionary<string, object?> _properties = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBuilder{TEvent}"/> class.
    /// </summary>
    public EventBuilder()
    {
        _event = new TEvent();
    }

    /// <summary>
    /// Sets a property on the event using reflection.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The builder for method chaining.</returns>
    public EventBuilder<TEvent> WithProperty(string propertyName, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        _properties[propertyName] = value;

        var property = typeof(TEvent).GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(_event, value);
        }

        return this;
    }

    /// <summary>
    /// Sets the event identifier.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>The builder for method chaining.</returns>
    public EventBuilder<TEvent> WithEventId(string eventId)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventId);

        var property = typeof(TEvent).GetProperty("EventId");
        if (property != null && property.CanWrite)
        {
            property.SetValue(_event, eventId);
        }

        return this;
    }

    /// <summary>
    /// Sets the timestamp when the event occurred.
    /// </summary>
    /// <param name="occurredOn">The timestamp.</param>
    /// <returns>The builder for method chaining.</returns>
    public EventBuilder<TEvent> WithOccurredOn(DateTime occurredOn)
    {
        var property = typeof(TEvent).GetProperty("OccurredOn");
        if (property != null && property.CanWrite)
        {
            property.SetValue(_event, occurredOn);
        }

        return this;
    }

    /// <summary>
    /// Sets the timestamp to the current UTC time.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public EventBuilder<TEvent> WithCurrentTimestamp()
    {
        return WithOccurredOn(DateTime.UtcNow);
    }

    /// <summary>
    /// Sets a custom timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp string.</param>
    /// <returns>The builder for method chaining.</returns>
    public EventBuilder<TEvent> WithTimestamp(string timestamp)
    {
        ArgumentException.ThrowIfNullOrEmpty(timestamp);

        if (DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return WithOccurredOn(dateTime);
        }

        return this;
    }

    /// <summary>
    /// Builds the event.
    /// </summary>
    /// <returns>The constructed event.</returns>
    public TEvent Build() => _event;

    /// <summary>
    /// Creates multiple events with the same configuration.
    /// </summary>
    /// <param name="count">The number of events to create.</param>
    /// <returns>A list of events.</returns>
    public List<TEvent> BuildMany(int count)
    {
        var events = new List<TEvent>();
        for (var i = 0; i < count; i++)
        {
            events.Add(Build());
        }

        return events;
    }

    /// <summary>
    /// Implicitly converts the builder to the event type.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed event.</returns>
    public static implicit operator TEvent(EventBuilder<TEvent> builder) => builder.Build();
}

/// <summary>
/// Non-generic event builder for creating domain events.
/// </summary>
public static class EventBuilder
{
    /// <summary>
    /// Creates a new event builder for the specified event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to build.</typeparam>
    /// <returns>A new event builder.</returns>
    public static EventBuilder<TEvent> Create<TEvent>()
        where TEvent : class, IDomainEvent, new()
    {
        return new EventBuilder<TEvent>();
    }

    /// <summary>
    /// Creates a new event with default values.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to create.</typeparam>
    /// <returns>A new event instance.</returns>
    public static TEvent CreateDefault<TEvent>()
        where TEvent : class, IDomainEvent, new()
    {
        return new TEvent();
    }

    /// <summary>
    /// Creates multiple events with default values.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to create.</typeparam>
    /// <param name="count">The number of events to create.</param>
    /// <returns>A list of events.</returns>
    public static List<TEvent> CreateMany<TEvent>(int count)
        where TEvent : class, IDomainEvent, new()
    {
        var events = new List<TEvent>();
        for (var i = 0; i < count; i++)
        {
            events.Add(new TEvent());
        }

        return events;
    }
}
