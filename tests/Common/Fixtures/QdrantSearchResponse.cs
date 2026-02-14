// <copyright file="QdrantSearchResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Synaxis.Common.Tests.Fixtures;

/// <summary>
/// Represents a Qdrant search response.
/// </summary>
internal class QdrantSearchResponse
{
    [JsonPropertyName("result")]
    public List<QdrantSearchResult>? Result { get; set; }
}
