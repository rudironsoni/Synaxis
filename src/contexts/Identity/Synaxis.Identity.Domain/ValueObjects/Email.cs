// <copyright file="Email.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.ValueObjects;

using System.Text.RegularExpressions;

/// <summary>
/// Represents a validated email address.
/// </summary>
public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));

    private Email(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the email address value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="Email"/> instance.
    /// </summary>
    /// <param name="value">The email address value.</param>
    /// <returns>A new <see cref="Email"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the email address is invalid.</exception>
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email cannot be null or whitespace.", nameof(value));
        }

        if (!EmailRegex.IsMatch(value))
        {
            throw new ArgumentException($"Invalid email address: {value}", nameof(value));
        }

        return new Email(value);
    }

    /// <summary>
    /// Returns the string representation of the email address.
    /// </summary>
    /// <returns>The email address value.</returns>
    public override string ToString() => this.Value;
}
