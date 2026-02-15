// <copyright file="AssertionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Testing;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Primitives;
using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Extension methods for fluent assertions on domain events and aggregates.
/// </summary>
public static class AssertionExtensions
{
    /// <summary>
    /// Asserts that a collection of domain events contains a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to find.</typeparam>
    /// <param name="events">The collection of events.</param>
    /// <returns>A continuation object for further assertions.</returns>
    public static AndWhichConstraint<ObjectAssertions, TEvent> ContainEvent<TEvent>(this IEnumerable<IDomainEvent> events)
        where TEvent : IDomainEvent
    {
        var eventList = events.ToList();
        var matchingEvent = eventList.OfType<TEvent>().FirstOrDefault();

        eventList.Should().ContainSingle(e => e is TEvent, $"Expected to find exactly one {typeof(TEvent).Name} event");

        return matchingEvent.Should().BeOfType<TEvent>();
    }

    /// <summary>
    /// Asserts that an aggregate has uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <returns>A continuation object for further assertions.</returns>
    public static AndConstraint<ObjectAssertions> HaveUncommittedEvents(this AggregateRoot aggregate)
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        uncommittedEvents.Should().NotBeEmpty("Aggregate should have uncommitted events");

        return aggregate.Should().NotBeNull();
    }

    /// <summary>
    /// Asserts that an aggregate has a specific number of uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedCount">The expected number of uncommitted events.</param>
    /// <returns>A continuation object for further assertions.</returns>
    public static AndConstraint<ObjectAssertions> HaveUncommittedEventsCount(this AggregateRoot aggregate, int expectedCount)
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        uncommittedEvents.Should().HaveCount(expectedCount, $"Expected {expectedCount} uncommitted events");

        return aggregate.Should().NotBeNull();
    }

    /// <summary>
    /// Asserts that an aggregate has no uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <returns>A continuation object for further assertions.</returns>
    public static AndConstraint<ObjectAssertions> HaveNoUncommittedEvents(this AggregateRoot aggregate)
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        uncommittedEvents.Should().BeEmpty("Aggregate should have no uncommitted events");

        return aggregate.Should().NotBeNull();
    }

    /// <summary>
    /// Asserts that an aggregate is at a specific version.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <returns>A continuation object for further assertions.</returns>
    public static AndConstraint<ObjectAssertions> BeAtVersion(this AggregateRoot aggregate, int expectedVersion)
    {
        aggregate.Version.Should().Be(expectedVersion, $"Expected aggregate to be at version {expectedVersion}");

        return aggregate.Should().NotBeNull();
    }

    /// <summary>
    /// Asserts that an event store contains events for a specific stream.
    /// </summary>
    /// <param name="eventStore">The event store to check.</param>
    /// <param name="streamId">The stream ID to check.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task HaveEventsForStreamAsync(this EventStore eventStore, string streamId)
    {
        var events = await eventStore.ReadStreamAsync(streamId);
        events.Should().NotBeEmpty($"Expected events to exist for stream {streamId}");
    }

    /// <summary>
    /// Asserts that an event store contains a specific number of events for a stream.
    /// </summary>
    /// <param name="eventStore">The event store to check.</param>
    /// <param name="streamId">The stream ID to check.</param>
    /// <param name="expectedCount">The expected number of events.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task HaveEventCountForStreamAsync(this EventStore eventStore, string streamId, int expectedCount)
    {
        var events = await eventStore.ReadStreamAsync(streamId);
        events.Should().HaveCount(expectedCount, $"Expected {expectedCount} events for stream {streamId}");
    }
}
