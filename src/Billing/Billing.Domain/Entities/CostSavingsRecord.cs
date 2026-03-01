// <copyright file="CostSavingsRecord.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Entities;

/// <summary>
/// Represents a cost savings record from ULTRA MISER MODE optimizations.
/// </summary>
public class CostSavingsRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for the cost savings record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the organization ID that owns this savings record.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the type of optimization applied.
    /// </summary>
    public string OptimizationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource type (API, Compute, Storage, etc.).
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource ID, if applicable.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the original cost before optimization.
    /// </summary>
    public decimal OriginalCost { get; set; }

    /// <summary>
    /// Gets or sets the optimized cost after optimization.
    /// </summary>
    public decimal OptimizedCost { get; set; }

    /// <summary>
    /// Gets or sets the savings amount.
    /// </summary>
    public decimal SavingsAmount { get; set; }

    /// <summary>
    /// Gets or sets the savings percentage.
    /// </summary>
    public decimal SavingsPercentage { get; set; }

    /// <summary>
    /// Gets or sets the strategy used for optimization.
    /// </summary>
    public string? StrategyUsed { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the optimization was applied.
    /// </summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this savings has been applied to an invoice.
    /// </summary>
    public bool IsAppliedToInvoice { get; set; }

    /// <summary>
    /// Gets or sets the invoice ID this savings was applied to.
    /// </summary>
    public Guid? AppliedInvoiceId { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
