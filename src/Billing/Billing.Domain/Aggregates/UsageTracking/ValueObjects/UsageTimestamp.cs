// <copyright file="UsageTimestamp.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Aggregates.UsageTracking.ValueObjects;

/// <summary>
/// Represents a validated timestamp for usage events.
/// </summary>
public record UsageTimestamp
{
    /// <summary>
    /// Gets the timestamp value.
    /// </summary>
    public DateTime Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageTimestamp"/> record.
    /// </summary>
    /// <param name="value">The timestamp value.</param>
    /// <exception cref="ArgumentException">Thrown when timestamp is too far in the future.</exception>
    public UsageTimestamp(DateTime value)
    {
        if (value > DateTime.UtcNow.AddMinutes(5))
            throw new ArgumentException("Usage timestamp cannot be in the future", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Gets the current UTC timestamp.
    /// </summary>
    public static UsageTimestamp Now => new(DateTime.UtcNow);
}
