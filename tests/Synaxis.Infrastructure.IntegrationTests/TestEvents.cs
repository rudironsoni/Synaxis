// <copyright file="TestEvents.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Sample domain event for testing.
/// </summary>
public sealed class TestEvent : IDomainEvent
{
    /// <inheritdoc />
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string EventType { get; set; } = "TestEvent";

    /// <summary>
    /// Gets or sets the test data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test value.
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// Another sample domain event for testing.
/// </summary>
public sealed class AnotherTestEvent : IDomainEvent
{
    /// <inheritdoc />
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string EventType { get; set; } = "AnotherTestEvent";

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public long Timestamp { get; set; }
}
