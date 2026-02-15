// <copyright file="TestAggregates.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Sample aggregate root for testing.
/// </summary>
public sealed class TestAggregate : AggregateRoot
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public int Value { get; private set; }

    /// <summary>
    /// Creates a new test aggregate.
    /// </summary>
    /// <param name="id">The aggregate ID.</param>
    /// <param name="name">The name.</param>
    /// <returns>A new test aggregate.</returns>
    public static TestAggregate Create(string id, string name)
    {
        var aggregate = new TestAggregate { Id = id };
        aggregate.ApplyEvent(new TestCreatedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = "TestCreatedEvent",
            OccurredOn = DateTime.UtcNow,
            Name = name
        });
        return aggregate;
    }

    /// <summary>
    /// Updates the value.
    /// </summary>
    /// <param name="newValue">The new value.</param>
    public void UpdateValue(int newValue)
    {
        this.ApplyEvent(new TestValueUpdatedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = "TestValueUpdatedEvent",
            OccurredOn = DateTime.UtcNow,
            NewValue = newValue
        });
    }

    /// <inheritdoc />
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case TestCreatedEvent created:
                this.Name = created.Name;
                break;
            case TestValueUpdatedEvent updated:
                this.Value = updated.NewValue;
                break;
        }
    }
}

/// <summary>
/// Event raised when a test aggregate is created.
/// </summary>
public sealed class TestCreatedEvent : IDomainEvent
{
    /// <inheritdoc />
    public string EventId { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTime OccurredOn { get; set; }

    /// <inheritdoc />
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Event raised when a test aggregate value is updated.
/// </summary>
public sealed class TestValueUpdatedEvent : IDomainEvent
{
    /// <inheritdoc />
    public string EventId { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTime OccurredOn { get; set; }

    /// <inheritdoc />
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new value.
    /// </summary>
    public int NewValue { get; set; }
}
