// <copyright file="CostOptimizationAgent.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Jobs
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Quartz;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Infrastructure.Agents.Tools;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Cost Optimization Agent (ULTRA MISER MODE) - Runs every 15 minutes.
    /// Priority 1: Find free alternatives ($0 cost)
    /// Priority 2: Find cheaper paid alternatives (>20% savings)
    /// Never: Free → Paid
    /// </summary>
    [DisallowConcurrentExecution]
    public class CostOptimizationAgent : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CostOptimizationAgent> _logger;

        public CostOptimizationAgent(IServiceProvider serviceProvider, ILogger<CostOptimizationAgent> logger)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogInformation("[CostOptimization][{CorrelationId}] Starting ULTRA MISER MODE optimization", correlationId);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var routingTool = scope.ServiceProvider.GetRequiredService<IRoutingTool>();
            var auditTool = scope.ServiceProvider.GetRequiredService<IAuditTool>();

            try
            {
                // Get all active routes from RequestLog (last 24 hours)
                var oneDayAgo = DateTime.UtcNow.AddDays(-1);
                var activeRoutes = await db.RequestLogs
                    .Where(r => r.CreatedAt >= oneDayAgo && !string.IsNullOrEmpty(r.Model) && !string.IsNullOrEmpty(r.Provider))
                    .GroupBy(r => new { r.TenantId, r.Model, r.Provider })
                    .Select(g => new
                    {
                        OrganizationId = g.Key.TenantId,
                        Model = g.Key.Model,
                        Provider = g.Key.Provider,
                        RequestCount = g.Count()
                    })
                    .ToListAsync(context.CancellationToken);

                _logger.LogInformation("[CostOptimization][{CorrelationId}] Found {Count} active routes",
                    correlationId, activeRoutes.Count);

                int optimizedCount = 0;
                decimal totalSavings = 0m;

                foreach (var route in activeRoutes)
                {
                    try
                    {
                        // Check if auto-optimization is enabled (TODO: implement hierarchical config check)
                        // For now, assume enabled

                        // Get current provider cost
                        var currentCost = await db.ModelCosts
                            .Where(mc => mc.Provider == route.Provider && mc.Model == route.Model)
                            .FirstOrDefaultAsync(context.CancellationToken);

                        if (currentCost == null)
                        {
                            _logger.LogDebug("[CostOptimization][{CorrelationId}] No cost data for {Provider}/{Model}",
                                correlationId, route.Provider, route.Model);
                            continue;
                        }

                        decimal currentCostPerToken = currentCost.CostPerToken;
                        bool currentIsFree = currentCost.FreeTier;

                        // ULTRA MISER MODE: Never switch from free to paid
                        if (currentIsFree)
                        {
                            _logger.LogDebug("[CostOptimization][{CorrelationId}] {Provider}/{Model} already free, skipping",
                                correlationId, route.Provider, route.Model);
                            continue;
                        }

                        // Find alternative providers for this model
                        var alternatives = await FindAlternativeProvidersAsync(
                            db,
                            route.OrganizationId,
                            route.Model!,
                            route.Provider!,
                            context.CancellationToken);

                        // Priority 1: Find FREE alternative ($0 cost)
                        var freeAlternative = alternatives.FirstOrDefault(a => a.IsFree && a.IsHealthy);
                        if (freeAlternative != null)
                        {
                            _logger.LogInformation(
                                "[CostOptimization][{CorrelationId}] ULTRA MISER: Found FREE alternative! {Old} → {New} for model {Model}",
                                correlationId, route.Provider, freeAlternative.Provider, route.Model);

                            bool switched = await routingTool.SwitchProviderAsync(
                                route.OrganizationId,
                                route.Model!,
                                route.Provider!,
                                freeAlternative.Provider,
                                "ULTRA MISER MODE: Switched to FREE provider",
                                context.CancellationToken);

                            if (switched)
                            {
                                await auditTool.LogOptimizationAsync(
                                    route.OrganizationId,
                                    route.Model!,
                                    route.Provider!,
                                    freeAlternative.Provider,
                                    100m, // 100% savings!
                                    "ULTRA MISER MODE: Switched to FREE provider",
                                    context.CancellationToken);

                                optimizedCount++;
                                totalSavings += currentCostPerToken;
                            }

                            continue;
                        }

                        // Priority 2: Find cheaper paid alternative (>20% savings)
                        foreach (var alternative in alternatives.Where(a => a.IsHealthy && !a.IsFree))
                        {
                            decimal altCostPerToken = alternative.CostPerToken;

                            // Calculate savings
                            decimal savings = currentCostPerToken > 0
                                ? (currentCostPerToken - altCostPerToken) / currentCostPerToken
                                : 0m;

                            // Require >20% savings
                            if (savings > 0.20m)
                            {
                                _logger.LogInformation(
                                    "[CostOptimization][{CorrelationId}] Found {Savings:P0} savings: {Old} → {New} for model {Model}",
                                    correlationId, savings, route.Provider, alternative.Provider, route.Model);

                                bool switched = await routingTool.SwitchProviderAsync(
                                    route.OrganizationId,
                                    route.Model!,
                                    route.Provider!,
                                    alternative.Provider,
                                    $"Cost optimization: {savings:P0} savings",
                                    context.CancellationToken);

                                if (switched)
                                {
                                    await auditTool.LogOptimizationAsync(
                                        route.OrganizationId,
                                        route.Model!,
                                        route.Provider!,
                                        alternative.Provider,
                                        savings * 100m,
                                        $"Cost optimization: {savings:P0} savings",
                                        context.CancellationToken);

                                    optimizedCount++;
                                    totalSavings += (currentCostPerToken - altCostPerToken);
                                }

                                break; // Take first qualifying alternative
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[CostOptimization][{CorrelationId}] Error optimizing route {Provider}/{Model}",
                            correlationId, route.Provider, route.Model);
                    }
                }

                _logger.LogInformation(
                    "[CostOptimization][{CorrelationId}] Completed: Optimized={Optimized}, Total Savings=${Savings:F4}",
                    correlationId, optimizedCount, totalSavings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CostOptimization][{CorrelationId}] Job failed", correlationId);
            }
        }

        private async Task<List<ProviderAlternative>> FindAlternativeProvidersAsync(
            ControlPlaneDbContext db,
            Guid organizationId,
            string modelId,
            string currentProvider,
            CancellationToken ct)
        {
            // Find all providers that support this model for this organization
            var alternatives = await db.ProviderModels
                .Where(pm => pm.GlobalModelId == modelId && pm.ProviderId != currentProvider && pm.IsAvailable)
                .Join(
                    db.Database.SqlQuery<OrgProviderDto>(
                        $"SELECT \"Id\", \"ProviderId\", \"IsEnabled\" FROM operations.\"OrganizationProvider\" WHERE \"OrganizationId\" = {organizationId} AND \"IsEnabled\" = true"
                    ),
                    pm => pm.ProviderId,
                    op => op.ProviderId.ToString(),
                    (pm, op) => new { pm.ProviderId, op.Id }
                )
                .ToListAsync(ct).ConfigureAwait(false);

            var result = new List<ProviderAlternative>();

            foreach (var alt in alternatives)
            {
                // Get cost for this provider
                var cost = await db.ModelCosts
                    .Where(mc => mc.Provider == alt.ProviderId && mc.Model == modelId)
                    .FirstOrDefaultAsync(ct).ConfigureAwait(false);

                // Get health status
                var health = await db.Database.SqlQuery<HealthDto>(
                    $"SELECT \"IsHealthy\" FROM operations.\"ProviderHealthStatus\" WHERE \"OrganizationProviderId\" = {alt.Id} ORDER BY \"LastCheckedAt\" DESC LIMIT 1"
                ).FirstOrDefaultAsync(ct).ConfigureAwait(false);

                result.Add(new ProviderAlternative
                {
                    Provider = alt.ProviderId,
                    CostPerToken = cost?.CostPerToken ?? 0m,
                    IsFree = cost?.FreeTier ?? false,
                    IsHealthy = health?.IsHealthy ?? true
                });
            }

            // Sort by cost (free first, then cheapest)
            return result.OrderByDescending(a => a.IsFree).ThenBy(a => a.CostPerToken).ToList();
        }

        private class ProviderAlternative
        {
            public string Provider { get; set; } = string.Empty;
            public decimal CostPerToken { get; set; }
            public bool IsFree { get; set; }
            public bool IsHealthy { get; set; }
        }

        private class OrgProviderDto
        {
            public Guid Id { get; set; }
            public Guid ProviderId { get; set; }
            public bool IsEnabled { get; set; }
        }

        private class HealthDto
        {
            public bool IsHealthy { get; set; }
        }
    }
}
