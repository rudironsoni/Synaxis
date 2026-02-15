// <copyright file="SemanticCacheResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

/// <summary>
/// Result of a semantic cache lookup operation.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public class SemanticCacheResult
{
    public bool IsHit { get; init; }

    public string? Response { get; init; }

    public float? SimilarityScore { get; init; }

    public float[]? QueryEmbedding { get; init; }

    public static SemanticCacheResult Hit(string response, float similarityScore)
        => new() { IsHit = true, Response = response, SimilarityScore = similarityScore };

    public static SemanticCacheResult Miss(float[]? embedding)
        => new() { IsHit = false, QueryEmbedding = embedding };
}
