// <copyright file="UsageMetadata.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Aggregates.UsageTracking.ValueObjects;

using System.Text.Json;

/// <summary>
/// Represents JSON metadata associated with a usage record.
/// </summary>
public record UsageMetadata
{
    /// <summary>
    /// Gets the JSON metadata string.
    /// </summary>
    public string Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageMetadata"/> record.
    /// </summary>
    /// <param name="data">The JSON metadata string.</param>
    public UsageMetadata(string data)
    {
        Data = data ?? "{}";
    }

    /// <summary>
    /// Creates a <see cref="UsageMetadata"/> from an object.
    /// </summary>
    /// <param name="data">The object to serialize.</param>
    /// <returns>A new metadata instance with serialized data.</returns>
    public static UsageMetadata FromObject(object data)
    {
        return new UsageMetadata(JsonSerializer.Serialize(data));
    }
}
