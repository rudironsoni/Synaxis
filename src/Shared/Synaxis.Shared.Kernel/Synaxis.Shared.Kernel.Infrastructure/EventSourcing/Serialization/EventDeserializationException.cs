// <copyright file="EventDeserializationException.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing.Serialization;

using System;

/// <summary>
/// Exception thrown when event deserialization fails.
/// </summary>
public class EventDeserializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventDeserializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public EventDeserializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventDeserializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EventDeserializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
