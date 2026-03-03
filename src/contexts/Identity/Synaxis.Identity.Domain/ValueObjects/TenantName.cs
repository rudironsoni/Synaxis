// <copyright file="TenantName.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.ValueObjects;

/// <summary>
/// Represents a validated tenant name.
/// </summary>
public sealed record TenantName
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    private TenantName(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the tenant name value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="TenantName"/> instance.
    /// </summary>
    /// <param name="value">The tenant name value.</param>
    /// <returns>A new <see cref="TenantName"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the tenant name is invalid.</exception>
    public static TenantName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tenant name cannot be null or whitespace.", nameof(value));
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length < MinLength)
        {
            throw new ArgumentException($"Tenant name must be at least {MinLength} characters.", nameof(value));
        }

        if (trimmedValue.Length > MaxLength)
        {
            throw new ArgumentException($"Tenant name cannot exceed {MaxLength} characters.", nameof(value));
        }

        return new TenantName(trimmedValue);
    }

    /// <summary>
    /// Returns the string representation of the tenant name.
    /// </summary>
    /// <returns>The tenant name value.</returns>
    public override string ToString() => this.Value;
}
