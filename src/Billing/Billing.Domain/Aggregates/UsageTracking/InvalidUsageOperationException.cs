// <copyright file="InvalidUsageOperationException.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Aggregates.UsageTracking;

/// <summary>
/// Exception thrown when an invalid usage operation is attempted.
/// </summary>
public class InvalidUsageOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidUsageOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidUsageOperationException(string message)
        : base(message)
    {
    }
}
