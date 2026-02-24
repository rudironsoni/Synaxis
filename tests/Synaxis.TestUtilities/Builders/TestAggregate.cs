// <copyright file="TestAggregate.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Builders;

using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Test aggregate for AutoFixture test data generation.
/// </summary>
public sealed class TestAggregate : AggregateRoot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestAggregate"/> class.
    /// </summary>
    public TestAggregate()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAggregate"/> class with a specific ID.
    /// </summary>
    /// <param name="id">The aggregate ID.</param>
    public TestAggregate(string id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets or sets a test property for aggregate state.
    /// </summary>
    public string TestProperty { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets a test numeric property for aggregate state.
    /// </summary>
    public int TestValue { get; private set; }

    /// <summary>
    /// Applies a test event to the aggregate.
    /// </summary>
    /// <param name="event">The test event to apply.</param>
    public void ApplyTestEvent(TestDomainEvent @event)
    {
        this.ApplyEvent(@event);
    }

    /// <inheritdoc />
    protected override void Apply(IDomainEvent @event)
    {
        if (@event is TestDomainEvent testEvent)
        {
            this.TestProperty = testEvent.PropertyValue;
            this.TestValue = testEvent.NumericValue;
        }
    }
}

/// <summary>
/// Test domain event for AutoFixture test data generation.
/// </summary>
public sealed class TestDomainEvent : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestDomainEvent"/> class.
    /// </summary>
    public TestDomainEvent()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDomainEvent"/> class with specific values.
    /// </summary>
    /// <param name="propertyValue">The property value.</param>
    /// <param name="numericValue">The numeric value.</param>
    public TestDomainEvent(string propertyValue, int numericValue)
    {
        this.PropertyValue = propertyValue;
        this.NumericValue = numericValue;
    }

    /// <inheritdoc />
    public override string AggregateId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the property value.
    /// </summary>
    public string PropertyValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric value.
    /// </summary>
    public int NumericValue { get; set; }
}
