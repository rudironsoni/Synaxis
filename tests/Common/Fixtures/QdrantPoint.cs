// <copyright file="QdrantPoint.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Fixtures
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

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
        public IDictionary<string, object>? Payload { get; set; }
    }
}
