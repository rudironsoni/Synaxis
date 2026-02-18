// <copyright file="AggregateAssertions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Assertions;

using FluentAssertions;
using Synaxis.Infrastructure.EventSourcing;
using Synaxis.TestUtilities.Aggregates;

/// <summary>
/// Provides extension methods for aggregate assertions in tests.
/// </summary>
public static class AggregateAssertions
{
    /// <summary>
    /// Asserts that the aggregate is in the expected state by comparing with the provided state.
    /// </summary>
    /// <typeparam name="TState">The type of the aggregate state.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedState">The expected state.</param>
    public static void ShouldBeInState<TState>(
        this TestAggregate<TState> aggregate,
        TState expectedState)
        where TState : class, new()
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(expectedState);

        var actualState = aggregate.GetState();
        actualState.Should().BeEquivalentTo(expectedState, "expected aggregate state to match");
    }

    /// <summary>
    /// Asserts that the aggregate is in the expected state matching a predicate.
    /// </summary>
    /// <typeparam name="TState">The type of the aggregate state.</typeparam>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="predicate">The predicate to match.</param>
    public static void ShouldBeInState<TState>(
        this TestAggregate<TState> aggregate,
        Func<TState, bool> predicate)
        where TState : class, new()
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(predicate);

        var actualState = aggregate.GetState();
        predicate(actualState).Should().BeTrue("expected aggregate state to satisfy the condition");
    }

    /// <summary>
    /// Asserts that the aggregate has uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    public static void ShouldHaveUncommittedEvents(this AggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.GetUncommittedEvents().Should().NotBeEmpty("expected aggregate to have uncommitted events");
    }

    /// <summary>
    /// Asserts that the aggregate has the expected number of uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedCount">The expected number of uncommitted events.</param>
    public static void ShouldHaveUncommittedEvents(
        this AggregateRoot aggregate,
        int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var uncommitted = aggregate.GetUncommittedEvents();
        uncommitted.Should().HaveCount(expectedCount, $"expected {expectedCount} uncommitted events");
    }

    /// <summary>
    /// Asserts that the aggregate has no uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    public static void ShouldHaveNoUncommittedEvents(this AggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.GetUncommittedEvents().Should().BeEmpty("expected aggregate to have no uncommitted events");
    }

    /// <summary>
    /// Asserts that the aggregate has events in its history.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    public static void ShouldHaveEvents(this TestAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.EventCount.Should().BeGreaterThan(0, "expected aggregate to have events");
    }

    /// <summary>
    /// Asserts that the aggregate has the expected number of events in its history.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedCount">The expected number of events.</param>
    public static void ShouldHaveEvents(this TestAggregate aggregate, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.EventCount.Should().Be(expectedCount, $"expected aggregate to have {expectedCount} events");
    }

    /// <summary>
    /// Asserts that the aggregate has no events in its history.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    public static void ShouldHaveNoEvents(this TestAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.EventCount.Should().Be(0, "expected aggregate to have no events");
    }

    /// <summary>
    /// Asserts that the aggregate is at the expected version.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedVersion">The expected version.</param>
    public static void ShouldBeAtVersion(
        this AggregateRoot aggregate,
        int expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.Version.Should().Be(expectedVersion, $"expected aggregate to be at version {expectedVersion}");
    }

    /// <summary>
    /// Asserts that the aggregate has the expected identifier.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedId">The expected identifier.</param>
    public static void ShouldHaveId(
        this AggregateRoot aggregate,
        string expectedId)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(expectedId);

        aggregate.Id.Should().Be(expectedId, $"expected aggregate to have id '{expectedId}'");
    }

    /// <summary>
    /// Asserts that the aggregate has a snapshot.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    public static void ShouldHaveSnapshot(this TestAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.HasSnapshot.Should().BeTrue("expected aggregate to have a snapshot");
    }

    /// <summary>
    /// Asserts that the aggregate has a snapshot at the expected version.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <param name="expectedVersion">The expected snapshot version.</param>
    public static void ShouldHaveSnapshotAtVersion(
        this TestAggregate aggregate,
        int expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.HasSnapshot.Should().BeTrue("expected aggregate to have a snapshot");
        aggregate.SnapshotVersion.Should().Be(expectedVersion, $"expected snapshot at version {expectedVersion}");
    }

    /// <summary>
    /// Asserts that the aggregate does not have a snapshot.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    public static void ShouldNotHaveSnapshot(this TestAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        aggregate.HasSnapshot.Should().BeFalse("expected aggregate not to have a snapshot");
    }
}
