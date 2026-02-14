// <copyright file="QdrantPoint.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Synaxis.Common.Tests.Fixtures;

/// <summary>
/// Represents a Qdrant point.
/// </summary>
public class QdrantPoint
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("vector")]
    public float[] Vector { get; set; } = System.Array.Empty<float>();

    [JsonPropertyName("payload")]
    public Dictionary<string, object>? Payload { get; set; }
}
