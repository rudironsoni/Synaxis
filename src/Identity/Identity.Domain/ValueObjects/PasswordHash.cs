// <copyright file="PasswordHash.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.ValueObjects;

/// <summary>
/// Represents a hashed password.
/// </summary>
public sealed record PasswordHash
{
    private PasswordHash(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the password hash value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="PasswordHash"/> instance.
    /// </summary>
    /// <param name="value">The password hash value.</param>
    /// <returns>A new <see cref="PasswordHash"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the password hash is null or whitespace.</exception>
    public static PasswordHash Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Password hash cannot be null or whitespace.", nameof(value));
        }

        return new PasswordHash(value);
    }

    /// <summary>
    /// Returns the string representation of the password hash.
    /// </summary>
    /// <returns>The password hash value.</returns>
    public override string ToString() => this.Value;
}
