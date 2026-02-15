// <copyright file="TokenOptimizationOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

/// <summary>
/// Token optimization configuration options for testing.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public class TokenOptimizationOptions
{
    public bool Enabled { get; set; }

    public bool SemanticCacheEnabled { get; set; }

    public float SemanticSimilarityThreshold { get; set; }

    public bool CompressionEnabled { get; set; }

    public string CompressionStrategy { get; set; } = string.Empty;

    public int MaxTokensBeforeCompression { get; set; }

    public bool SessionAffinityEnabled { get; set; }

    public int SessionAffinityTtlHours { get; set; }

    public bool DeduplicationEnabled { get; set; }

    public int DeduplicationTtlSeconds { get; set; }
}
