// <copyright file="QdrantSearchResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Fixtures;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a Qdrant search response.
/// </summary>
internal class QdrantSearchResponse
{
    [JsonPropertyName("result")]
    public List<QdrantSearchResult>? Result { get; set; }
}
