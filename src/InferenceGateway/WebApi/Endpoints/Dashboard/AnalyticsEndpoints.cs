// <copyright file="AnalyticsEndpoints.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Dashboard
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Endpoints for analytics data.
    /// </summary>
    public static class AnalyticsEndpoints
    {
        /// <summary>
        /// Maps analytics endpoints to the application route builder.
        /// </summary>
        /// <param name="app">The endpoint route builder.</param>
        /// <returns>The endpoint route builder with analytics endpoints configured.</returns>
        public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
        {
            var analyticsGroup = app.MapGroup("/api/analytics")
                .RequireAuthorization(policy => policy.RequireAuthenticatedUser())
                .RequireCors("WebApp");

            MapUsageAnalyticsEndpoint(analyticsGroup);
            MapProviderAnalyticsEndpoint(analyticsGroup);

            return app;
        }

        private static void MapUsageAnalyticsEndpoint(RouteGroupBuilder analyticsGroup)
        {
            analyticsGroup.MapGet("/usage", async (
                ControlPlaneDbContext dbContext,
                string? startDate,
                string? endDate,
                CancellationToken ct) =>
            {
                var end = ParseEndDate(endDate);
                var start = ParseStartDate(startDate, end);

                var response = await BuildUsageAnalyticsResponse(dbContext, start, end, ct);
                return Results.Ok(response);
            })
            .WithTags("Analytics")
            .WithSummary("Get usage analytics")
            .WithDescription("Returns aggregated token usage and request statistics for the specified time range");
        }

        private static void MapProviderAnalyticsEndpoint(RouteGroupBuilder analyticsGroup)
        {
            analyticsGroup.MapGet("/providers", async (
                ControlPlaneDbContext dbContext,
                CancellationToken ct) =>
            {
                var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
                var providers = await BuildProviderAnalyticsList(dbContext, yesterday, ct);

                return Results.Ok(new ProviderAnalyticsResponseDto
                {
                    Providers = providers,
                });
            })
            .WithTags("Analytics")
            .WithSummary("Get provider analytics")
            .WithDescription("Returns performance metrics and usage statistics for all providers over the last 24 hours");
        }

        private static async Task<UsageAnalyticsDto> BuildUsageAnalyticsResponse(
            ControlPlaneDbContext dbContext,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken ct)
        {
            var tokenUsageData = await GetTokenUsageData(dbContext, start, end, ct);
            var providerData = await GetProviderUsageData(dbContext, start, end, ct);
            await PopulateProviderTokens(dbContext, providerData, ct);

            return new UsageAnalyticsDto
            {
                TotalTokens = tokenUsageData?.TotalTokens ?? 0,
                TotalRequests = tokenUsageData?.TotalRequests ?? 0,
                Providers = providerData,
                TimeRange = new TimeRangeDto
                {
                    Start = start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    End = end.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                },
            };
        }

        private static async Task<List<ProviderAnalyticsDto>> BuildProviderAnalyticsList(
            ControlPlaneDbContext dbContext,
            DateTimeOffset yesterday,
            CancellationToken ct)
        {
            var providerStats = await GetProviderStats(dbContext, yesterday, ct);
            var providers = new List<ProviderAnalyticsDto>();

            foreach (var stat in providerStats)
            {
                var dailyTokens = await GetProviderDailyTokens(dbContext, stat.ProviderId!, yesterday, ct);
                var successRate = CalculateSuccessRate(stat.TotalRequests, stat.SuccessfulRequests);

                providers.Add(new ProviderAnalyticsDto
                {
                    Id = stat.ProviderId!,
                    Performance = new ProviderPerformanceDto
                    {
                        AvgResponseTime = (int)Math.Round(stat.AverageLatency),
                        SuccessRate = Math.Round(successRate, 4),
                    },
                    Usage = new ProviderDailyUsageDto
                    {
                        DailyTokens = dailyTokens,
                        DailyRequests = stat.TotalRequests,
                    },
                });
            }

            return providers;
        }

        private static async Task<dynamic?> GetTokenUsageData(
            ControlPlaneDbContext dbContext,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken ct)
        {
            return await dbContext.TokenUsages
                .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
                .GroupBy(t => 1)
                .Select(g => new
                {
                    TotalTokens = g.Sum(t => t.InputTokens + t.OutputTokens),
                    TotalRequests = g.Count(),
                })
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }

        private static async Task<List<ProviderUsageStatsDto>> GetProviderUsageData(
            ControlPlaneDbContext dbContext,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken ct)
        {
            return await dbContext.RequestLogs
                .Where(r => r.CreatedAt >= start && r.CreatedAt <= end && r.Provider != null)
                .GroupBy(r => r.Provider)
                .Select(g => new ProviderUsageStatsDto
                {
                    Id = g.Key!,
                    Requests = g.Count(),
                    Tokens = 0,
                })
                .ToListAsync(ct).ConfigureAwait(false);
        }

        private static async Task PopulateProviderTokens(
            ControlPlaneDbContext dbContext,
            List<ProviderUsageStatsDto> providerData,
            CancellationToken ct)
        {
            foreach (var provider in providerData)
            {
                var providerTokens = await dbContext.TokenUsages
                    .Join(
                        dbContext.RequestLogs.Where(r => r.Provider == provider.Id),
                        tu => tu.RequestId,
                        rl => rl.RequestId,
                        (tu, rl) => tu.InputTokens + tu.OutputTokens)
                    .SumAsync(ct).ConfigureAwait(false);

                provider.Tokens = providerTokens;
            }
        }

        private static async Task<List<ProviderStatsInternal>> GetProviderStats(
            ControlPlaneDbContext dbContext,
            DateTimeOffset yesterday,
            CancellationToken ct)
        {
            return await dbContext.RequestLogs
                .Where(r => r.CreatedAt >= yesterday && r.Provider != null)
                .GroupBy(r => r.Provider)
                .Select(g => new ProviderStatsInternal
                {
                    ProviderId = g.Key!,
                    TotalRequests = g.Count(),
                    SuccessfulRequests = g.Count(r => r.StatusCode >= 200 && r.StatusCode < 300),
                    AverageLatency = g.Average(r => r.LatencyMs ?? 0),
                })
                .ToListAsync(ct).ConfigureAwait(false);
        }

        private static async Task<long> GetProviderDailyTokens(
            ControlPlaneDbContext dbContext,
            string providerId,
            DateTimeOffset yesterday,
            CancellationToken ct)
        {
            return await dbContext.TokenUsages
                .Join(
                    dbContext.RequestLogs.Where(r => r.Provider == providerId && r.CreatedAt >= yesterday),
                    tu => tu.RequestId,
                    rl => rl.RequestId,
                    (tu, rl) => tu.InputTokens + tu.OutputTokens)
                .SumAsync(ct).ConfigureAwait(false);
        }

        private static DateTimeOffset ParseEndDate(string? endDate)
        {
            return string.IsNullOrEmpty(endDate)
                ? DateTimeOffset.UtcNow
                : DateTimeOffset.Parse(endDate, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static DateTimeOffset ParseStartDate(string? startDate, DateTimeOffset endDate)
        {
            return string.IsNullOrEmpty(startDate)
                ? endDate.AddDays(-30)
                : DateTimeOffset.Parse(startDate, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static double CalculateSuccessRate(int totalRequests, int successfulRequests)
        {
            return totalRequests > 0
                ? (double)successfulRequests / totalRequests
                : 0.0;
        }
    }

    /// <summary>
    /// DTO for usage analytics data.
    /// </summary>
    public class UsageAnalyticsDto
    {
        /// <summary>
        /// Gets or sets the total number of tokens used.
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// Gets or sets the total number of requests.
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets the provider usage statistics.
        /// </summary>
        public ICollection<ProviderUsageStatsDto> Providers { get; set; } = new List<ProviderUsageStatsDto>();

        /// <summary>
        /// Gets or sets the time range for the analytics data.
        /// </summary>
        public TimeRangeDto TimeRange { get; set; } = new TimeRangeDto();
    }

    /// <summary>
    /// DTO for provider usage statistics.
    /// </summary>
    public class ProviderUsageStatsDto
    {
        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of tokens used.
        /// </summary>
        public long Tokens { get; set; }

        /// <summary>
        /// Gets or sets the number of requests.
        /// </summary>
        public int Requests { get; set; }
    }

    /// <summary>
    /// DTO for time range information.
    /// </summary>
    public class TimeRangeDto
    {
        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public string Start { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public string End { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for provider analytics.
    /// </summary>
    public class ProviderAnalyticsResponseDto
    {
        /// <summary>
        /// Gets or sets the list of provider analytics.
        /// </summary>
        public ICollection<ProviderAnalyticsDto> Providers { get; set; } = new List<ProviderAnalyticsDto>();
    }

    /// <summary>
    /// DTO for provider analytics data.
    /// </summary>
    public class ProviderAnalyticsDto
    {
        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the performance metrics.
        /// </summary>
        public ProviderPerformanceDto Performance { get; set; } = new ProviderPerformanceDto();

        /// <summary>
        /// Gets or sets the usage metrics.
        /// </summary>
        public ProviderDailyUsageDto Usage { get; set; } = new ProviderDailyUsageDto();
    }

    /// <summary>
    /// DTO for provider performance metrics.
    /// </summary>
    public class ProviderPerformanceDto
    {
        /// <summary>
        /// Gets or sets the average response time in milliseconds.
        /// </summary>
        public int AvgResponseTime { get; set; }

        /// <summary>
        /// Gets or sets the success rate percentage.
        /// </summary>
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// DTO for provider daily usage metrics.
    /// </summary>
    public class ProviderDailyUsageDto
    {
        /// <summary>
        /// Gets or sets the daily token usage.
        /// </summary>
        public long DailyTokens { get; set; }

        /// <summary>
        /// Gets or sets the daily request count.
        /// </summary>
        public int DailyRequests { get; set; }
    }

    /// <summary>
    /// Internal DTO for provider statistics aggregation.
    /// </summary>
    internal class ProviderStatsInternal
    {
        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        public string? ProviderId { get; set; }

        /// <summary>
        /// Gets or sets the total number of requests.
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets the number of successful requests.
        /// </summary>
        public int SuccessfulRequests { get; set; }

        /// <summary>
        /// Gets or sets the average latency.
        /// </summary>
        public double AverageLatency { get; set; }
    }
}
