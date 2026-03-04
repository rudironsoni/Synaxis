// <copyright file="ConcurrencyException.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing;

using System;

/// <summary>
/// Exception thrown when optimistic concurrency check fails during event append.
/// </summary>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="actualVersion">The actual version.</param>
    public ConcurrencyException(string streamId, long expectedVersion, long actualVersion)
        : base($"Concurrency conflict in stream '{streamId}'. Expected version: {expectedVersion}, but actual version is: {actualVersion}.")
    {
        this.StreamId = streamId;
        this.ExpectedVersion = expectedVersion;
        this.ActualVersion = actualVersion;
    }

    /// <summary>
    /// Gets the stream identifier where the conflict occurred.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Gets the expected version that was provided.
    /// </summary>
    public long ExpectedVersion { get; }

    /// <summary>
    /// Gets the actual version found in the stream.
    /// </summary>
    public long ActualVersion { get; }
}
