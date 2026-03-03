// <copyright file="TokenUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents token usage for an inference request.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Gets or sets the prompt tokens.
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the completion tokens.
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Gets the total tokens.
    /// </summary>
    public int TotalTokens => this.PromptTokens + this.CompletionTokens;
}
