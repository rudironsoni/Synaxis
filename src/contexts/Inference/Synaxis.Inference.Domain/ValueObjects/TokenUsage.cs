// <copyright file="TokenUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.ValueObjects;

/// <summary>
/// Represents token usage for an inference request.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenUsage"/> class.
    /// </summary>
    /// <param name="inputTokenCount">The number of input tokens.</param>
    /// <param name="outputTokenCount">The number of output tokens.</param>
    public TokenUsage(int inputTokenCount, int outputTokenCount)
    {
        this.InputTokenCount = inputTokenCount;
        this.OutputTokenCount = outputTokenCount;
    }

    /// <summary>
    /// Gets the number of input tokens.
    /// </summary>
    public int InputTokenCount { get; }

    /// <summary>
    /// Gets the number of output tokens.
    /// </summary>
    public int OutputTokenCount { get; }

    /// <summary>
    /// Gets the total number of tokens.
    /// </summary>
    public int TotalTokens => this.InputTokenCount + this.OutputTokenCount;
}
