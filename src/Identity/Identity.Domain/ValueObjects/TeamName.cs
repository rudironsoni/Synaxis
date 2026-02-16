// <copyright file="TeamName.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.ValueObjects;

/// <summary>
/// Represents a validated team name.
/// </summary>
public sealed record TeamName
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    private TeamName(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the team name value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="TeamName"/> instance.
    /// </summary>
    /// <param name="value">The team name value.</param>
    /// <returns>A new <see cref="TeamName"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the team name is invalid.</exception>
    public static TeamName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Team name cannot be null or whitespace.", nameof(value));
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length < MinLength)
        {
            throw new ArgumentException($"Team name must be at least {MinLength} characters.", nameof(value));
        }

        if (trimmedValue.Length > MaxLength)
        {
            throw new ArgumentException($"Team name cannot exceed {MaxLength} characters.", nameof(value));
        }

        return new TeamName(trimmedValue);
    }

    /// <summary>
    /// Returns the string representation of the team name.
    /// </summary>
    /// <returns>The team name value.</returns>
    public override string ToString() => this.Value;
}
