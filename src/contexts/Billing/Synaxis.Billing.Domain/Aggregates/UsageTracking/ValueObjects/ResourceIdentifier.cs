// <copyright file="ResourceIdentifier.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Aggregates.UsageTracking.ValueObjects;

/// <summary>
/// Represents a validated resource identifier.
/// </summary>
public record ResourceIdentifier
{
    /// <summary>
    /// Gets the identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceIdentifier"/> record.
    /// </summary>
    /// <param name="value">The identifier value.</param>
    /// <exception cref="ArgumentException">Thrown when value is empty.</exception>
    public ResourceIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Resource identifier cannot be empty", nameof(value));

        Value = value;
    }
}
