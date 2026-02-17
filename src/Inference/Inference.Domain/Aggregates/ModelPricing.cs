// <copyright file="ModelPricing.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents model pricing.
/// </summary>
public class ModelPricing
{
    /// <summary>
    /// Gets or sets the input token price per 1K tokens.
    /// </summary>
    public decimal InputPricePer1K { get; set; }

    /// <summary>
    /// Gets or sets the output token price per 1K tokens.
    /// </summary>
    public decimal OutputPricePer1K { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a free tier model.
    /// </summary>
    public bool IsFreeTier { get; set; }

    /// <summary>
    /// Gets or sets the free tier quota.
    /// </summary>
    public int? FreeTierQuota { get; set; }

    /// <summary>
    /// Calculates the cost for token usage.
    /// </summary>
    /// <param name="inputTokens">The number of input tokens.</param>
    /// <param name="outputTokens">The number of output tokens.</param>
    /// <returns>The calculated cost.</returns>
    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        if (this.IsFreeTier)
        {
            return 0m;
        }

        var inputCost = (inputTokens / 1000m) * this.InputPricePer1K;
        var outputCost = (outputTokens / 1000m) * this.OutputPricePer1K;
        return inputCost + outputCost;
    }
}
