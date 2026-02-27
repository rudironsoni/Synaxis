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
    using Npgsql;
    using Quartz;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Infrastructure.Agents.Tools;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Cost Optimization Agent (ULTRA MISER MODE) - Runs every 15 minutes.
    /// Priority 1: Find free alternatives ($0 cost)
    /// Priority 2: Find cheaper paid alternatives (>20% savings)
    /// Never: Free → Paid.
    /// </summary>
    [DisallowConcurrentExecution]
    public class CostOptimizationAgent : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CostOptimizationAgent> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CostOptimizationAgent"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        public CostOptimizationAgent(IServiceProvider serviceProvider, ILogger<CostOptimizationAgent> logger)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
        }

        /// <summary>
        /// Executes the cost optimization job to find free or cheaper provider alternatives.
        /// </summary>
        /// <param name="context">The job execution context.</param>
        /// <returns>A task that represents the asynchronous job execution.</returns>
        /// <inheritdoc/>
        public async Task Execute(IJobExecutionContext context)
        {
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            this._logger.LogInformation("[CostOptimization][{CorrelationId}] Starting ULTRA MISER MODE optimization", correlationId);

            using var scope = this._serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var routingTool = scope.ServiceProvider.GetRequiredService<IRoutingTool>();
            var auditTool = scope.ServiceProvider.GetRequiredService<IAuditTool>();

            try
            {
                var activeRoutes = await this.GetActiveRoutesAsync(db, context.CancellationToken).ConfigureAwait(false);

                this._logger.LogInformation(
                    "[CostOptimization][{CorrelationId}] Found {Count} active routes",
                    correlationId,
                    activeRoutes.Count);

                int optimizedCount = 0;
                decimal totalSavings = 0m;

                foreach (var route in activeRoutes)
                {
                    try
                    {
                        var result = await this.OptimizeRouteAsync(db, routingTool, auditTool, route, correlationId, context.CancellationToken).ConfigureAwait(false);
                        optimizedCount += result.Optimized ? 1 : 0;
                        totalSavings += result.Savings;
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(
                            ex,
                            "[CostOptimization][{CorrelationId}] Error optimizing route {Provider}/{Model}",
                            correlationId,
                            route.Provider,
                            route.Model);
                    }
                }

                this._logger.LogInformation(
                    "[CostOptimization][{CorrelationId}] Completed: Optimized={Optimized}, Total Savings=${Savings:F4}",
                    correlationId,
                    optimizedCount,
                    totalSavings);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "[CostOptimization][{CorrelationId}] Job failed", correlationId);
            }
        }

        private Task<List<ActiveRoute>> GetActiveRoutesAsync(ControlPlaneDbContext db, CancellationToken ct)
        {
            var oneDayAgo = DateTimeOffset.UtcNow.AddDays(-1);
            return db.RequestLogs
                .Where(r => r.CreatedAt >= oneDayAgo && !string.IsNullOrEmpty(r.Model) && !string.IsNullOrEmpty(r.Provider))
                .GroupBy(r => new { r.TenantId, r.Model, r.Provider })
                .Select(g => new ActiveRoute
                {
                    OrganizationId = g.Key.TenantId,
                    Model = g.Key.Model,
                    Provider = g.Key.Provider,
                    RequestCount = g.Count(),
                })
                .ToListAsync(ct);
        }

        private async Task<OptimizationResult> OptimizeRouteAsync(
            ControlPlaneDbContext db,
            IRoutingTool routingTool,
            IAuditTool auditTool,
            ActiveRoute route,
            string correlationId,
            CancellationToken ct)
        {
            var currentCost = await db.ModelCosts
                .Where(mc => mc.Provider == route.Provider && mc.Model == route.Model)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);

            if (currentCost == null)
            {
                this._logger.LogDebug(
                    "[CostOptimization][{CorrelationId}] No cost data for {Provider}/{Model}",
                    correlationId,
                    route.Provider,
                    route.Model);
                return new OptimizationResult { Optimized = false, Savings = 0m };
            }

            if (currentCost.FreeTier)
            {
                this._logger.LogDebug(
                    "[CostOptimization][{CorrelationId}] {Provider}/{Model} already free, skipping",
                    correlationId,
                    route.Provider,
                    route.Model);
                return new OptimizationResult { Optimized = false, Savings = 0m };
            }

            var alternatives = await this.FindAlternativeProvidersAsync(db, route.OrganizationId, route.Model!, route.Provider!, ct).ConfigureAwait(false);

            var result = await this.TryFreeAlternativeAsync(routingTool, auditTool, route, alternatives, currentCost.CostPerToken, correlationId, ct).ConfigureAwait(false);
            if (result.Optimized)
            {
                return result;
            }

            return await this.TryCheaperAlternativeAsync(routingTool, auditTool, route, alternatives, currentCost.CostPerToken, correlationId, ct).ConfigureAwait(false);
        }

        private async Task<OptimizationResult> TryFreeAlternativeAsync(
            IRoutingTool routingTool,
            IAuditTool auditTool,
            ActiveRoute route,
            List<ProviderAlternative> alternatives,
            decimal currentCostPerToken,
            string correlationId,
            CancellationToken ct)
        {
            var freeAlternative = alternatives.FirstOrDefault(a => a.IsFree && a.IsHealthy);
            if (freeAlternative == null)
            {
                return new OptimizationResult { Optimized = false, Savings = 0m };
            }

            this._logger.LogInformation(
                "[CostOptimization][{CorrelationId}] ULTRA MISER: Found FREE alternative! {Old} → {New} for model {Model}",
                correlationId,
                route.Provider,
                freeAlternative.Provider,
                route.Model);

            bool switched = await routingTool.SwitchProviderAsync(
                route.OrganizationId,
                route.Model!,
                route.Provider!,
                freeAlternative.Provider,
                "ULTRA MISER MODE: Switched to FREE provider",
                ct).ConfigureAwait(false);

            if (switched)
            {
                await auditTool.LogOptimizationAsync(
                    route.OrganizationId,
                    route.Model!,
                    route.Provider!,
                    freeAlternative.Provider,
                    100m,
                    "ULTRA MISER MODE: Switched to FREE provider",
                    ct).ConfigureAwait(false);

                return new OptimizationResult { Optimized = true, Savings = currentCostPerToken };
            }

            return new OptimizationResult { Optimized = false, Savings = 0m };
        }

        private async Task<OptimizationResult> TryCheaperAlternativeAsync(
            IRoutingTool routingTool,
            IAuditTool auditTool,
            ActiveRoute route,
            List<ProviderAlternative> alternatives,
            decimal currentCostPerToken,
            string correlationId,
            CancellationToken ct)
        {
            foreach (var alternative in alternatives.Where(a => a.IsHealthy && !a.IsFree))
            {
                decimal savings = currentCostPerToken > 0
                    ? (currentCostPerToken - alternative.CostPerToken) / currentCostPerToken
                    : 0m;

                if (savings > 0.20m)
                {
                    this._logger.LogInformation(
                        "[CostOptimization][{CorrelationId}] Found {Savings:P0} savings: {Old} → {New} for model {Model}",
                        correlationId,
                        savings,
                        route.Provider,
                        alternative.Provider,
                        route.Model);

                    bool switched = await routingTool.SwitchProviderAsync(
                        route.OrganizationId,
                        route.Model!,
                        route.Provider!,
                        alternative.Provider,
                        $"Cost optimization: {savings:P0} savings",
                        ct).ConfigureAwait(false);

                    if (switched)
                    {
                        await auditTool.LogOptimizationAsync(
                            route.OrganizationId,
                            route.Model!,
                            route.Provider!,
                            alternative.Provider,
                            savings * 100m,
                            $"Cost optimization: {savings:P0} savings",
                            ct).ConfigureAwait(false);

                        return new OptimizationResult { Optimized = true, Savings = currentCostPerToken - alternative.CostPerToken };
                    }

                    break;
                }
            }

            return new OptimizationResult { Optimized = false, Savings = 0m };
        }

        private sealed class ActiveRoute
        {
            /// <summary>
            /// Gets or sets the OrganizationId.
            /// </summary>
            public Guid OrganizationId { get; set; }

            /// <summary>
            /// Gets or sets the Model.
            /// </summary>
            public string? Model { get; set; }

            /// <summary>
            /// Gets or sets the Provider.
            /// </summary>
            public string? Provider { get; set; }

            /// <summary>
            /// Gets or sets the RequestCount.
            /// </summary>
            public int RequestCount { get; set; }
        }

        private sealed class OptimizationResult
        {
            /// <summary>
            /// Gets or sets a value indicating whether the optimization was successful.
            /// </summary>
            public bool Optimized { get; set; }

            /// <summary>
            /// Gets or sets the Savings.
            /// </summary>
            public decimal Savings { get; set; }
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
                        $"SELECT \"Id\", \"ProviderId\", \"IsEnabled\" FROM operations.\"OrganizationProvider\" WHERE \"OrganizationId\" = ${{organizationId}} AND \"IsEnabled\" = true",
                        new[] { new Npgsql.NpgsqlParameter("organizationId", organizationId) }),
                    pm => pm.ProviderId,
                    op => op.ProviderId.ToString(),
                    (pm, op) => new { pm.ProviderId, op.Id })
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
                    $"SELECT \"IsHealthy\" FROM operations.\"ProviderHealthStatus\" WHERE \"OrganizationProviderId\" = ${{organizationProviderId}} ORDER BY \"LastCheckedAt\" DESC LIMIT 1",
                    new[] { new Npgsql.NpgsqlParameter("organizationProviderId", alt.Id) })
                    .FirstOrDefaultAsync(ct).ConfigureAwait(false);

                result.Add(new ProviderAlternative
                {
                    Provider = alt.ProviderId,
                    CostPerToken = cost?.CostPerToken ?? 0m,
                    IsFree = cost?.FreeTier ?? false,
                    IsHealthy = health?.IsHealthy ?? true,
                });
            }

            // Sort by cost (free first, then cheapest)
            return result.OrderByDescending(a => a.IsFree).ThenBy(a => a.CostPerToken).ToList();
        }

        private sealed class ProviderAlternative
        {
            /// <summary>
            /// Gets or sets the Provider.
            /// </summary>
            public string Provider { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the CostPerToken.
            /// </summary>
            public decimal CostPerToken { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the provider offers free tier access.
            /// </summary>
            public bool IsFree { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the provider is currently healthy.
            /// </summary>
            public bool IsHealthy { get; set; }
        }

        private sealed class OrgProviderDto
        {
            /// <summary>
            /// Gets or sets the Id.
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            /// Gets or sets the ProviderId.
            /// </summary>
            public Guid ProviderId { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the provider is enabled.
            /// </summary>
            public bool IsEnabled { get; set; }
        }

        private sealed class HealthDto
        {
            /// <summary>
            /// Gets or sets a value indicating whether the provider is healthy.
            /// </summary>
            public bool IsHealthy { get; set; }
        }
    }
}
