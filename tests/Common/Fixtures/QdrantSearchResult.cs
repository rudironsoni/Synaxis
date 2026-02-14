// <copyright file="QdrantSearchResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Synaxis.Common.Tests.Fixtures;

/// <summary>
/// Represents a Qdrant search result.
/// </summary>
public class QdrantSearchResult
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("payload")]
    public Dictionary<string, object>? Payload { get; set; }
}
