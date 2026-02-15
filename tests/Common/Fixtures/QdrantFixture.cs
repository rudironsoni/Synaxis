// <copyright file="QdrantFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Fixtures;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Testcontainers.Qdrant;
using Xunit;

#pragma warning disable IDISP003 // False positive: fields are only assigned once in InitializeAsync
#pragma warning disable IDISP001 // HttpResponseMessage is properly disposed by using statements
#pragma warning disable IDISP017 // HttpResponseMessage is properly disposed by using statements

/// <summary>
/// Shared Qdrant fixture for integration tests.
/// Manages a single Qdrant container for the test assembly.
/// </summary>
public sealed class QdrantFixture : IAsyncLifetime, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private QdrantContainer? _container;
    private HttpClient? _httpClient;

    /// <summary>
    /// Gets the Qdrant endpoint URL.
    /// </summary>
    public string Endpoint => _container != null
        ? $"http://{_container.Hostname}:{_container.GetMappedPublicPort(6333)}"
        : throw new InvalidOperationException("Qdrant container not initialized");

    /// <summary>
    /// Gets the Qdrant container.
    /// </summary>
    public QdrantContainer Container => _container
        ?? throw new InvalidOperationException("Qdrant container not initialized");

    /// <summary>
    /// Gets the HTTP client for Qdrant API calls.
    /// </summary>
    public HttpClient HttpClient => _httpClient
        ?? throw new InvalidOperationException("HTTP client not initialized");

    /// <summary>
    /// Initializes the Qdrant container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        _container = new QdrantBuilder("qdrant/qdrant:latest")
            .WithPortBinding(6333, true)
            .Build();

        await _container.StartAsync();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(6333)}")
        };
    }

    /// <summary>
    /// Disposes the Qdrant container and HTTP client.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    /// <summary>
    /// Creates a new collection in Qdrant.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="vectorSize">The size of the vectors.</param>
    /// <param name="distance">The distance metric (Cosine, Euclid, or Dot).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateCollectionAsync(string collectionName, int vectorSize, string distance = "Cosine")
    {
        var payload = new
        {
            vectors = new
            {
                size = vectorSize,
                distance = distance
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await HttpClient.PutAsync($"/collections/{collectionName}?wait=true", content);
        response.EnsureSuccessStatusCode();

        // Ensure collection is queryable before returning.
        for (var retry = 0; retry < 10; retry++)
        {
            if (await this.CollectionExistsAsync(collectionName).ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(100).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Collection '{collectionName}' was not available after creation.");
    }

    /// <summary>
    /// Deletes a collection from Qdrant.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteCollectionAsync(string collectionName)
    {
        using var response = await HttpClient.DeleteAsync($"/collections/{collectionName}");
        // Ignore 404 - collection might not exist
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Checks if a collection exists.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the collection exists, false otherwise.</returns>
    public async Task<bool> CollectionExistsAsync(string collectionName)
    {
        using var response = await HttpClient.GetAsync($"/collections/{collectionName}");
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Upserts points into a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="points">The points to upsert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpsertPointsAsync(string collectionName, IList<QdrantPoint> points)
    {
        var payload = new
        {
            points = points
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await HttpClient.PutAsync($"/collections/{collectionName}/points?wait=true", content);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Qdrant upsert failed: {(int)response.StatusCode} {response.StatusCode}. Body: {errorBody}");
        }
    }

    /// <summary>
    /// Searches for points in a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="vector">The query vector.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <param name="scoreThreshold">The minimum similarity score threshold.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the search results.</returns>
    public async Task<IList<QdrantSearchResult>> SearchAsync(
        string collectionName,
        float[] vector,
        int limit = 10,
        float scoreThreshold = 0.0f)
    {
        var payload = new
        {
            vector = vector,
            limit = limit,
            score_threshold = scoreThreshold,
            with_payload = true
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await HttpClient.PostAsync($"/collections/{collectionName}/points/search", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<QdrantSearchResponse>(json, JsonOptions);

        return result?.Result ?? new List<QdrantSearchResult>();
    }

    /// <summary>
    /// Deletes all points from a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteAllPointsAsync(string collectionName)
    {
        var payload = new
        {
            filter = new { }
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await HttpClient.PostAsync($"/collections/{collectionName}/points/delete", content);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets the number of points in a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of points in the collection.</returns>
    public async Task<long> GetPointCountAsync(string collectionName)
    {
        using var response = await HttpClient.GetAsync($"/collections/{collectionName}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);
        return result.GetProperty("result").GetProperty("points_count").GetInt64();
    }
}
