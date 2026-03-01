// <copyright file="UltraMiserOptimizationHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.EventHandlers;

using Billing.Domain.Entities;
using Billing.Infrastructure.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Notification for when a ULTRA MISER MODE optimization is applied.
/// </summary>
public record OptimizationApplied : INotification
{
    /// <summary>
    /// Gets the organization ID.
    /// </summary>
    public Guid OrganizationId { get; init; }

    /// <summary>
    /// Gets the type of optimization applied.
    /// </summary>
    public string OptimizationType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resource type (API, Compute, Storage, etc.).
    /// </summary>
    public string ResourceType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resource ID, if applicable.
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// Gets the original cost before optimization.
    /// </summary>
    public decimal OriginalCost { get; init; }

    /// <summary>
    /// Gets the optimized cost after optimization.
    /// </summary>
    public decimal OptimizedCost { get; init; }

    /// <summary>
    /// Gets the savings amount.
    /// </summary>
    public decimal SavingsAmount => OriginalCost - OptimizedCost;

    /// <summary>
    /// Gets the savings percentage.
    /// </summary>
    public decimal SavingsPercentage => OriginalCost > 0 ? (SavingsAmount / OriginalCost) * 100 : 0;

    /// <summary>
    /// Gets the timestamp when the optimization was applied.
    /// </summary>
    public DateTime AppliedAt { get; init; }

    /// <summary>
    /// Gets the strategy used for optimization.
    /// </summary>
    public string? StrategyUsed { get; init; }
}

/// <summary>
/// Handler for <see cref="OptimizationApplied"/> notifications.
/// Records cost savings from ULTRA MISER MODE optimizations.
/// </summary>
public class UltraMiserOptimizationHandler : INotificationHandler<OptimizationApplied>
{
    private readonly ICostSavingsRepository _costSavingsRepository;
    private readonly IUsageRecordRepository _usageRepository;
    private readonly ILogger<UltraMiserOptimizationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UltraMiserOptimizationHandler"/> class.
    /// </summary>
    /// <param name="costSavingsRepository">The cost savings repository.</param>
    /// <param name="usageRepository">The usage record repository.</param>
    /// <param name="logger">The logger.</param>
    public UltraMiserOptimizationHandler(
        ICostSavingsRepository costSavingsRepository,
        IUsageRecordRepository usageRepository,
        ILogger<UltraMiserOptimizationHandler> logger)
    {
        _costSavingsRepository = costSavingsRepository ?? throw new ArgumentNullException(nameof(costSavingsRepository));
        _usageRepository = usageRepository ?? throw new ArgumentNullException(nameof(usageRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task Handle(OptimizationApplied notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification.SavingsAmount <= 0)
        {
            _logger.LogDebug("No savings from optimization, skipping");
            return;
        }

        // Check for duplicate (idempotency)
        var existing = await _costSavingsRepository.GetByOptimizationDetailsAsync(
            notification.OrganizationId,
            notification.ResourceType,
            notification.ResourceId,
            notification.AppliedAt,
            cancellationToken);

        if (existing != null)
        {
            _logger.LogDebug("Duplicate optimization detected, skipping");
            return;
        }

        // Record the cost savings
        var costSavings = new CostSavingsRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = notification.OrganizationId,
            OptimizationType = notification.OptimizationType,
            ResourceType = notification.ResourceType,
            ResourceId = notification.ResourceId,
            OriginalCost = notification.OriginalCost,
            OptimizedCost = notification.OptimizedCost,
            SavingsAmount = notification.SavingsAmount,
            SavingsPercentage = notification.SavingsPercentage,
            StrategyUsed = notification.StrategyUsed,
            AppliedAt = notification.AppliedAt,
            IsAppliedToInvoice = false,
        };

        await _costSavingsRepository.AddAsync(costSavings, cancellationToken);

        _logger.LogInformation(
            "Recorded ULTRA MISER MODE savings: {OptimizationType} on {ResourceType} saved {SavingsAmount:C} ({SavingsPercentage:F1}%)",
            notification.OptimizationType,
            notification.ResourceType,
            notification.SavingsAmount,
            notification.SavingsPercentage);
    }
}
