// <copyright file="ConcurrencyException.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict is detected during event persistence.
/// </summary>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    public ConcurrencyException()
        : base("A concurrency conflict was detected while attempting to append events.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConcurrencyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with stream and version information.
    /// </summary>
    /// <param name="streamId">The identifier of the event stream.</param>
    /// <param name="expectedVersion">The expected version of the stream.</param>
    /// <param name="actualVersion">The actual version of the stream.</param>
    public ConcurrencyException(string streamId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict detected for stream '{streamId}'. Expected version {expectedVersion}, but actual version is {actualVersion}.")
    {
        this.StreamId = streamId;
        this.ExpectedVersion = expectedVersion;
        this.ActualVersion = actualVersion;
    }

    /// <summary>
    /// Gets the identifier of the event stream where the conflict occurred.
    /// </summary>
    public string? StreamId { get; }

    /// <summary>
    /// Gets the expected version of the stream.
    /// </summary>
    public int ExpectedVersion { get; }

    /// <summary>
    /// Gets the actual version of the stream.
    /// </summary>
    public int ActualVersion { get; }
}
