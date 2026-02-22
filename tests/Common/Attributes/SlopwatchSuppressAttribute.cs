// <copyright file="SlopwatchSuppressAttribute.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Attributes;

/// <summary>
/// Attribute to suppress slopwatch warnings/errors when justified.
/// Use sparingly and always document why the suppression is necessary.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public sealed class SlopwatchSuppressAttribute : Attribute
{
    /// <summary>
    /// Gets the rule code being suppressed (e.g., "SW001", "SW002").
    /// </summary>
    public string Rule { get; }

    /// <summary>
    /// Gets the justification for the suppression (must be 20+ characters).
    /// </summary>
    public string Justification { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlopwatchSuppressAttribute"/> class.
    /// </summary>
    /// <param name="rule">The rule code to suppress (e.g., "SW001").</param>
    /// <param name="justification">Explanation of why suppression is necessary (20+ chars required).</param>
    public SlopwatchSuppressAttribute(string rule, string justification)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(justification);

        if (justification.Length < 20)
        {
            throw new ArgumentException("Justification must be at least 20 characters", nameof(justification));
        }

        this.Rule = rule;
        this.Justification = justification;
    }
}
