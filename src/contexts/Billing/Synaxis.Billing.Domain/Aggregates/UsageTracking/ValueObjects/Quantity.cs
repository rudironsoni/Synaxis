// <copyright file="Quantity.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Aggregates.UsageTracking.ValueObjects;

/// <summary>
/// Represents a measured quantity with its unit of measurement.
/// </summary>
public record Quantity
{
    /// <summary>
    /// Gets the numeric value of the quantity.
    /// </summary>
    public decimal Value { get; }

    /// <summary>
    /// Gets the unit of measurement.
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Quantity"/> record.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <exception cref="ArgumentException">Thrown when value is negative or unit is empty.</exception>
    public Quantity(decimal value, string unit)
    {
        if (value < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(value));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit is required", nameof(unit));

        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Creates a zero quantity with the specified unit.
    /// </summary>
    /// <param name="unit">The unit of measurement.</param>
    /// <returns>A zero quantity.</returns>
    public static Quantity Zero(string unit) => new(0, unit);

    /// <summary>
    /// Adds another quantity to this one.
    /// </summary>
    /// <param name="other">The quantity to add.</param>
    /// <returns>A new quantity representing the sum.</returns>
    /// <exception cref="InvalidOperationException">Thrown when units do not match.</exception>
    public Quantity Add(Quantity other)
    {
        if (Unit != other.Unit)
            throw new InvalidOperationException("Cannot add quantities with different units");
        return new Quantity(Value + other.Value, Unit);
    }
}
