// <copyright file="EventAssertions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Assertions;

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Collections;
using Synaxis.Abstractions.Cloud;
using Synaxis.TestUtilities.Aggregates;

/// <summary>
/// Provides extension methods for event assertions in tests.
/// </summary>
public static class EventAssertions
{
    /// <summary>
    /// Asserts that the aggregate contains at least one event of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check for.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    public static void ShouldContainEvent<TEvent>(this TestAggregate aggregate)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.ContainsEvent<TEvent>().Should().BeTrue($"expected aggregate to contain event of type {typeof(TEvent).Name}");
    }

    /// <summary>
    /// Asserts that the aggregate contains an event of the specified type matching the predicate.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check for.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="predicate">The predicate to match.</param>
    public static void ShouldContainEvent<TEvent>(
        this TestAggregate aggregate,
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1204", Justification = "Required for assertion")]
        Func<TEvent, bool> predicate)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(predicate);

        aggregate.ContainsEvent(predicate).Should().BeTrue($"expected aggregate to contain matching event of type {typeof(TEvent).Name}");
    }

    /// <summary>
    /// Asserts that the aggregate does not contain any event of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check for.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    public static void ShouldNotContainEvent<TEvent>(this TestAggregate aggregate)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.ContainsEvent<TEvent>().Should().BeFalse($"expected aggregate not to contain event of type {typeof(TEvent).Name}");
    }

    /// <summary>
    /// Asserts that the aggregate does not contain an event matching the predicate.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check for.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="predicate">The predicate to check against.</param>
    public static void ShouldNotContainEvent<TEvent>(
        this TestAggregate aggregate,
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1204", Justification = "Required for assertion")]
        Func<TEvent, bool> predicate)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(predicate);

        var matchingEvents = aggregate.GetEvents<TEvent>().Where(predicate);
        matchingEvents.Should().BeEmpty($"expected aggregate not to contain matching event of type {typeof(TEvent).Name}");
    }

    /// <summary>
    /// Asserts that the event collection contains an event of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check for.</typeparam>
    /// <param name="events">The events to check.</param>
    public static void ShouldContainEvent<TEvent>(
        this IEnumerable<IDomainEvent> events)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        eventList.Should().Contain(e => e is TEvent, $"expected events to contain type {typeof(TEvent).Name}");
    }

    /// <summary>
    /// Asserts that the event collection does not contain an event of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check for.</typeparam>
    /// <param name="events">The events to check.</param>
    public static void ShouldNotContainEvent<TEvent>(
        this IEnumerable<IDomainEvent> events)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        eventList.Should().NotContain(e => e is TEvent, $"expected events not to contain type {typeof(TEvent).Name}");
    }

    /// <summary>
    /// Asserts that the event collection contains exactly the specified number of events.
    /// </summary>
    /// <param name="events">The events to check.</param>
    /// <param name="expectedCount">The expected number of events.</param>
    public static void ShouldHaveEventCount(
        this IEnumerable<IDomainEvent> events,
        int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        eventList.Should().HaveCount(expectedCount, $"expected exactly {expectedCount} events");
    }

    /// <summary>
    /// Asserts that the event collection contains exactly the specified number of events of the given type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to count.</typeparam>
    /// <param name="events">The events to check.</param>
    /// <param name="expectedCount">The expected number of events.</param>
    public static void ShouldHaveEventCount<TEvent>(
        this IEnumerable<IDomainEvent> events,
        int expectedCount)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        var count = eventList.OfType<TEvent>().Count();
        count.Should().Be(expectedCount, $"expected exactly {expectedCount} events of type {typeof(TEvent).Name}");
    }

    /// <summary>
    /// Asserts that the event collection is empty.
    /// </summary>
    /// <param name="events">The events to check.</param>
    public static void ShouldHaveNoEvents(
        this IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        eventList.Should().BeEmpty("expected no events");
    }

    /// <summary>
    /// Asserts that the event collection is not empty.
    /// </summary>
    /// <param name="events">The events to check.</param>
    public static void ShouldHaveEvents(
        this IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        eventList.Should().NotBeEmpty("expected at least one event");
    }
}
