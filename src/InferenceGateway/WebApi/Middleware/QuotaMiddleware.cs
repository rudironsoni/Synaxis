using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.InferenceGateway.Application.Interfaces;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Middleware;

/// <summary>
/// Middleware that enforces quota limits for organizations and API keys.
/// Handles rate limiting, budget enforcement, and quota exhaustion.
/// </summary>
public sealed class QuotaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<QuotaMiddleware> _logger;

    public QuotaMiddleware(
        RequestDelegate next,
        ILogger<QuotaMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to enforce quota limits.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IQuotaService quotaService)
    {
        try
        {
            // Skip quota checks for health and public endpoints
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/openapi") ||
                context.Request.Path.StartsWithSegments("/identity"))
            {
                await _next(context);
                return;
            }

            // Only enforce quotas if tenant context is established
            if (tenantContext.OrganizationId == null)
            {
                await _next(context);
                return;
            }

            // Check request quota
            var quotaCheckRequest = new QuotaCheckRequest
            {
                MetricType = "requests",
                IncrementBy = 1,
                TimeGranularity = "minute",
                WindowType = WindowType.Sliding
            };

            QuotaResult quotaResult;
            if (tenantContext.UserId.HasValue)
            {
                quotaResult = await quotaService.CheckUserQuotaAsync(
                    tenantContext.OrganizationId.Value,
                    tenantContext.UserId.Value,
                    quotaCheckRequest);
            }
            else
            {
                quotaResult = await quotaService.CheckQuotaAsync(
                    tenantContext.OrganizationId.Value,
                    quotaCheckRequest);
            }

            // Handle quota result
            switch (quotaResult.Action)
            {
                case QuotaAction.Block:
                    _logger.LogWarning(
                        "Request blocked due to quota exhaustion. OrgId: {OrgId}, Reason: {Reason}",
                        tenantContext.OrganizationId, quotaResult.Reason);

                    context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            message = quotaResult.Reason ?? "Quota exceeded",
                            type = "quota_exceeded",
                            code = "QUOTA_EXHAUSTED"
                        }
                    });
                    return;

                case QuotaAction.Throttle:
                    _logger.LogWarning(
                        "Request throttled due to rate limit. OrgId: {OrgId}, Limit: {Limit}, Current: {Current}",
                        tenantContext.OrganizationId, 
                        quotaResult.Details?.Limit,
                        quotaResult.Details?.CurrentUsage);

                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    
                    // Add rate limit headers
                    if (quotaResult.Details != null)
                    {
                        context.Response.Headers["X-RateLimit-Limit"] = quotaResult.Details.Limit.ToString();
                        context.Response.Headers["X-RateLimit-Remaining"] = quotaResult.Details.Remaining.ToString();
                        context.Response.Headers["X-RateLimit-Reset"] = quotaResult.Details.WindowEnd.ToString("R");
                        
                        if (quotaResult.Details.RetryAfter.HasValue)
                        {
                            context.Response.Headers["Retry-After"] = ((int)quotaResult.Details.RetryAfter.Value.TotalSeconds).ToString();
                        }
                    }

                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            message = "Rate limit exceeded",
                            type = "rate_limit_exceeded",
                            code = "RATE_LIMIT_EXCEEDED",
                            retry_after = quotaResult.Details?.RetryAfter?.TotalSeconds
                        }
                    });
                    return;

                case QuotaAction.CreditCharge:
                    _logger.LogInformation(
                        "Request will incur credit charge. OrgId: {OrgId}, Amount: {Amount}",
                        tenantContext.OrganizationId, quotaResult.CreditCharge);

                    // Add credit charge header
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers["X-Credit-Charge"] = quotaResult.CreditCharge?.ToString("F4") ?? "0";
                        return Task.CompletedTask;
                    });
                    break;

                case QuotaAction.Allow:
                    // Request is allowed, continue
                    break;
            }

            // Store quota info in context for usage tracking
            context.Items["QuotaResult"] = quotaResult;

            await _next(context);

            // After request completes, increment usage
            if (context.Response.StatusCode < 400)
            {
                await quotaService.IncrementUsageAsync(
                    tenantContext.OrganizationId.Value,
                    new UsageMetrics
                    {
                        UserId = tenantContext.UserId,
                        VirtualKeyId = tenantContext.ApiKeyId,
                        MetricType = "requests",
                        Value = 1,
                        Model = context.Items["ModelName"] as string
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during quota enforcement");
            await _next(context);
        }
    }
}
