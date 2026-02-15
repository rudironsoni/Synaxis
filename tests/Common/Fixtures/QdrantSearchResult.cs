// <copyright file="QdrantSearchResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Fixtures
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

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
        public IDictionary<string, object>? Payload { get; set; }
    }
}
