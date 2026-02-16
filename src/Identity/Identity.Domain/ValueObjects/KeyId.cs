// <copyright file="KeyId.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for an API key.
/// </summary>
public sealed record KeyId
{
    private KeyId(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the key identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="KeyId"/> instance.
    /// </summary>
    /// <param name="value">The key identifier value.</param>
    /// <returns>A new <see cref="KeyId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the key identifier is null or whitespace.</exception>
    public static KeyId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Key identifier cannot be null or whitespace.", nameof(value));
        }

        return new KeyId(value);
    }

    /// <summary>
    /// Generates a new unique key identifier.
    /// </summary>
    /// <returns>A new <see cref="KeyId"/> instance with a unique value.</returns>
    public static KeyId Generate()
    {
        return new KeyId(Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    /// Returns the string representation of the key identifier.
    /// </summary>
    /// <returns>The key identifier value.</returns>
    public override string ToString() => this.Value;
}
