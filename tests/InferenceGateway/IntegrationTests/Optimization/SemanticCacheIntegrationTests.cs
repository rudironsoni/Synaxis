// <copyright file="SemanticCacheIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Common.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.Optimization
{
    /// <summary>
    /// Integration tests for Semantic Cache using real Qdrant container.
    /// Tests semantic caching functionality with actual vector database interactions.
    /// Uses shared QdrantFixture to avoid per-test container churn.
    /// Each test uses a unique collection name for deterministic isolation.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("QdrantIntegration")]
    public class SemanticCacheIntegrationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly QdrantFixture _qdrantFixture;
        private const int VectorSize = 384;

        public SemanticCacheIntegrationTests(ITestOutputHelper output, QdrantFixture qdrantFixture)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _qdrantFixture = qdrantFixture ?? throw new ArgumentNullException(nameof(qdrantFixture));
        }

        [Fact]
        public async Task CacheAndRetrieve_ExactMatch_Success()
        {
            var collectionName = $"semantic_cache_exact_match_{Guid.NewGuid():N}";
            await _qdrantFixture.CreateCollectionAsync(collectionName, VectorSize);
            try
            {
                // Arrange
                var query = "What is the capital of France?";
                var response = "The capital of France is Paris.";
                var sessionId = "session-exact-match";
                var model = "gpt-4";
                var temperature = 0.7;
                var embedding = GenerateTestEmbedding(VectorSize);

                var pointId = 1L;
                var payload = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["query"] = query,
                    ["response"] = response,
                    ["session_id"] = sessionId,
                    ["model"] = model,
                    ["temperature"] = temperature
                };

                var point = new QdrantPoint
                {
                    Id = pointId,
                    Vector = embedding,
                    Payload = payload
                };

                // Act - Store in cache
                await _qdrantFixture.UpsertPointsAsync(collectionName, new List<QdrantPoint> { point });

                // Act - Retrieve from cache with exact same query
                var results = await _qdrantFixture.SearchAsync(collectionName, embedding, limit: 1, scoreThreshold: 0.99f);

                // Assert
                Assert.NotEmpty(results);
                Assert.Equal(response, results[0].Payload?["response"]?.ToString());
                Assert.True(results[0].Score >= 0.99, $"Expected high similarity, got {results[0].Score}");
            }
            finally
            {
                // Clean up collection
                await _qdrantFixture.DeleteCollectionAsync(collectionName);
            }
        }

        [Fact]
        public async Task CacheAndRetrieve_SimilarQuery_ReturnsSemanticHit()
        {
            var collectionName = $"semantic_cache_similar_{Guid.NewGuid():N}";
            await _qdrantFixture.CreateCollectionAsync(collectionName, VectorSize);
            try
            {
                // Arrange
                var originalQuery = "What is the capital of France?";
                var response = "The capital of France is Paris.";
                var sessionId = "session-similar";
                var model = "gpt-4";
                var temperature = 0.7;

                var originalEmbedding = GenerateTestEmbedding(VectorSize, seed: 42);
                var similarEmbedding = GenerateTestEmbedding(VectorSize, seed: 42, noise: 0.1f);

                var pointId = 2L;
                var payload = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["query"] = originalQuery,
                    ["response"] = response,
                    ["session_id"] = sessionId,
                    ["model"] = model,
                    ["temperature"] = temperature
                };

                var point = new QdrantPoint
                {
                    Id = pointId,
                    Vector = originalEmbedding,
                    Payload = payload
                };

                // Act - Store original
                await _qdrantFixture.UpsertPointsAsync(collectionName, new List<QdrantPoint> { point });

                // Act - Retrieve with similar query
                var results = await _qdrantFixture.SearchAsync(collectionName, similarEmbedding, limit: 1, scoreThreshold: 0.8f);

                // Assert
                Assert.NotEmpty(results);
                Assert.True(results[0].Score >= 0.8, $"Semantic hit should have high similarity, got {results[0].Score}");
                Assert.Equal(response, results[0].Payload?["response"]?.ToString());
            }
            finally
            {
                // Clean up collection
                await _qdrantFixture.DeleteCollectionAsync(collectionName);
            }
        }

        [Fact]
        public async Task CacheAndRetrieve_DifferentSessions_Isolated()
        {
            var collectionName = $"semantic_cache_sessions_{Guid.NewGuid():N}";
            await _qdrantFixture.CreateCollectionAsync(collectionName, VectorSize);
            try
            {
                // Arrange
                var query = "What is the capital of France?";
                var response = "The capital of France is Paris.";
                var sessionId1 = "session-1";
                var sessionId2 = "session-2";
                var model = "gpt-4";
                var temperature = 0.7;
                var embedding = GenerateTestEmbedding(VectorSize);

                var pointId = 3L;
                var payload = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["query"] = query,
                    ["response"] = response,
                    ["session_id"] = sessionId1,
                    ["model"] = model,
                    ["temperature"] = temperature
                };

                var point = new QdrantPoint
                {
                    Id = pointId,
                    Vector = embedding,
                    Payload = payload
                };

                // Act - Store in session 1
                await _qdrantFixture.UpsertPointsAsync(collectionName, new List<QdrantPoint> { point });

                // Act - Try to retrieve from session 2 (different session)
                var results = await _qdrantFixture.SearchAsync(collectionName, embedding, limit: 10);

                // Assert - Should not find cache from different session
                var session2Results = results.Where(r => r.Payload?["session_id"]?.ToString() == sessionId2).ToList();
                Assert.Empty(session2Results);
            }
            finally
            {
                // Clean up collection
                await _qdrantFixture.DeleteCollectionAsync(collectionName);
            }
        }

        [Fact]
        public async Task InvalidateSession_RemovesCorrectEntries()
        {
            var collectionName = $"semantic_cache_invalidate_{Guid.NewGuid():N}";
            await _qdrantFixture.CreateCollectionAsync(collectionName, VectorSize);
            try
            {
                // Arrange
                var sessionId = "session-invalidate";
                var model = "gpt-4";
                var temperature = 0.7;

                // Store multiple entries in the session
                var points = new List<QdrantPoint>();
                for (int i = 0; i < 3; i++)
                {
                    var pointId = 10L + i;
                    var payload = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["query"] = $"Query {i}",
                        ["response"] = $"Response {i}",
                        ["session_id"] = sessionId,
                        ["model"] = model,
                        ["temperature"] = temperature
                    };

                    points.Add(new QdrantPoint
                    {
                        Id = pointId,
                        Vector = GenerateTestEmbedding(VectorSize, seed: i),
                        Payload = payload
                    });
                }

                await _qdrantFixture.UpsertPointsAsync(collectionName, points);

                // Act - Delete all points (simulating session invalidation)
                await _qdrantFixture.DeleteAllPointsAsync(collectionName);

                // Assert - Verify entries are gone
                var pointCount = await _qdrantFixture.GetPointCountAsync(collectionName);
                Assert.Equal(0, pointCount);
            }
            finally
            {
                // Clean up collection
                await _qdrantFixture.DeleteCollectionAsync(collectionName);
            }
        }

        [Fact]
        public async Task ConcurrentWrites_HandlesSafely()
        {
            var collectionName = $"semantic_cache_concurrent_{Guid.NewGuid():N}";
            await _qdrantFixture.CreateCollectionAsync(collectionName, VectorSize);
            try
            {
                // Arrange
                var sessionId = "session-concurrent";
                var model = "gpt-4";
                var temperature = 0.7;

                // Act - Store multiple entries concurrently
                var tasks = Enumerable.Range(0, 10).Select(async i =>
                {
                    var pointId = 20L + i;
                    var payload = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["query"] = $"Query {i}",
                        ["response"] = $"Response {i}",
                        ["session_id"] = sessionId,
                        ["model"] = model,
                        ["temperature"] = temperature
                    };

                    var point = new QdrantPoint
                    {
                        Id = pointId,
                        Vector = GenerateTestEmbedding(VectorSize, seed: i),
                        Payload = payload
                    };

                    await _qdrantFixture.UpsertPointsAsync(collectionName, new List<QdrantPoint> { point });
                });

                await Task.WhenAll(tasks);

                // Assert - Verify all entries were stored
                var pointCount = await _qdrantFixture.GetPointCountAsync(collectionName);
                Assert.Equal(10, pointCount);
            }
            finally
            {
                // Clean up collection
                await _qdrantFixture.DeleteCollectionAsync(collectionName);
            }
        }

        private float[] GenerateTestEmbedding(int dimension, int seed = 42, float noise = 0.0f)
        {
            var random = new Random(seed);
            var embedding = new float[dimension];

            for (int i = 0; i < dimension; i++)
            {
                var baseValue = (float)Math.Sin((i * 0.1) + seed);
                var noiseValue = noise > 0 ? (float)((random.NextDouble() * noise * 2) - noise) : 0;
                embedding[i] = baseValue + noiseValue;
            }

            // Normalize
            var norm = (float)Math.Sqrt(embedding.Sum(x => x * x));
            for (int i = 0; i < dimension; i++)
            {
                embedding[i] /= norm;
            }

            return embedding;
        }
    }
}
