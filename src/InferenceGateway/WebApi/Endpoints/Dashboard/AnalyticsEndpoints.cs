using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Dashboard;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var analyticsGroup = app.MapGroup("/api/analytics")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser())
            .RequireCors("WebApp");

        analyticsGroup.MapGet("/usage", async (
            ControlPlaneDbContext dbContext,
            string? startDate,
            string? endDate,
            CancellationToken ct) =>
        {
            var end = ParseEndDate(endDate);
            var start = ParseStartDate(startDate, end);

            var tokenUsageData = await dbContext.TokenUsages
                .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
                .GroupBy(t => 1)
                .Select(g => new
                {
                    TotalTokens = g.Sum(t => t.InputTokens + t.OutputTokens),
                    TotalRequests = g.Count()
                })
                .FirstOrDefaultAsync(ct);

            var providerData = await dbContext.RequestLogs
                .Where(r => r.CreatedAt >= start && r.CreatedAt <= end && r.Provider != null)
                .GroupBy(r => r.Provider)
                .Select(g => new ProviderUsageStatsDto
                {
                    Id = g.Key!,
                    Requests = g.Count(),
                    Tokens = 0
                })
                .ToListAsync(ct);

            foreach (var provider in providerData)
            {
                var providerTokens = await dbContext.TokenUsages
                    .Join(
                        dbContext.RequestLogs.Where(r => r.Provider == provider.Id),
                        tu => tu.RequestId,
                        rl => rl.RequestId,
                        (tu, rl) => tu.InputTokens + tu.OutputTokens)
                    .SumAsync(ct);
                
                provider.Tokens = providerTokens;
            }

            var response = new UsageAnalyticsDto
            {
                TotalTokens = tokenUsageData?.TotalTokens ?? 0,
                TotalRequests = tokenUsageData?.TotalRequests ?? 0,
                Providers = providerData,
                TimeRange = new TimeRangeDto
                {
                    Start = start.ToString("yyyy-MM-dd"),
                    End = end.ToString("yyyy-MM-dd")
                }
            };

            return Results.Ok(response);
        })
        .WithTags("Analytics")
        .WithSummary("Get usage analytics")
        .WithDescription("Returns aggregated token usage and request statistics for the specified time range");

        analyticsGroup.MapGet("/providers", async (
            ControlPlaneDbContext dbContext,
            CancellationToken ct) =>
        {
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
            
            var providerStats = await dbContext.RequestLogs
                .Where(r => r.CreatedAt >= yesterday && r.Provider != null)
                .GroupBy(r => r.Provider)
                .Select(g => new
                {
                    ProviderId = g.Key!,
                    TotalRequests = g.Count(),
                    SuccessfulRequests = g.Count(r => r.StatusCode >= 200 && r.StatusCode < 300),
                    AverageLatency = g.Average(r => r.LatencyMs ?? 0)
                })
                .ToListAsync(ct);

            var providers = new List<ProviderAnalyticsDto>();

            foreach (var stat in providerStats)
            {
                var dailyTokens = await dbContext.TokenUsages
                    .Join(
                        dbContext.RequestLogs.Where(r => r.Provider == stat.ProviderId && r.CreatedAt >= yesterday),
                        tu => tu.RequestId,
                        rl => rl.RequestId,
                        (tu, rl) => tu.InputTokens + tu.OutputTokens)
                    .SumAsync(ct);

                var successRate = CalculateSuccessRate(stat.TotalRequests, stat.SuccessfulRequests);

                providers.Add(new ProviderAnalyticsDto
                {
                    Id = stat.ProviderId,
                    Performance = new ProviderPerformanceDto
                    {
                        AvgResponseTime = (int)Math.Round(stat.AverageLatency),
                        SuccessRate = Math.Round(successRate, 4)
                    },
                    Usage = new ProviderDailyUsageDto
                    {
                        DailyTokens = dailyTokens,
                        DailyRequests = stat.TotalRequests
                    }
                });
            }

            return Results.Ok(new ProviderAnalyticsResponseDto
            {
                Providers = providers
            });
        })
        .WithTags("Analytics")
        .WithSummary("Get provider analytics")
        .WithDescription("Returns performance metrics and usage statistics for all providers over the last 24 hours");

        return app;
    }

    private static DateTimeOffset ParseEndDate(string? endDate)
    {
        return string.IsNullOrEmpty(endDate) 
            ? DateTimeOffset.UtcNow 
            : DateTimeOffset.Parse(endDate);
    }

    private static DateTimeOffset ParseStartDate(string? startDate, DateTimeOffset endDate)
    {
        return string.IsNullOrEmpty(startDate) 
            ? endDate.AddDays(-30) 
            : DateTimeOffset.Parse(startDate);
    }

    private static double CalculateSuccessRate(int totalRequests, int successfulRequests)
    {
        return totalRequests > 0 
            ? (double)successfulRequests / totalRequests 
            : 0.0;
    }
}
public class UsageAnalyticsDto
{
    public long TotalTokens { get; set; }
    public int TotalRequests { get; set; }
    public List<ProviderUsageStatsDto> Providers { get; set; } = new();
    public TimeRangeDto TimeRange { get; set; } = new();
}

public class ProviderUsageStatsDto
{
    public string Id { get; set; } = "";
    public long Tokens { get; set; }
    public int Requests { get; set; }
}

public class TimeRangeDto
{
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
}

public class ProviderAnalyticsResponseDto
{
    public List<ProviderAnalyticsDto> Providers { get; set; } = new();
}

public class ProviderAnalyticsDto
{
    public string Id { get; set; } = "";
    public ProviderPerformanceDto Performance { get; set; } = new();
    public ProviderDailyUsageDto Usage { get; set; } = new();
}

public class ProviderPerformanceDto
{
    public int AvgResponseTime { get; set; }
    public double SuccessRate { get; set; }
}

public class ProviderDailyUsageDto
{
    public long DailyTokens { get; set; }
    public int DailyRequests { get; set; }
}
