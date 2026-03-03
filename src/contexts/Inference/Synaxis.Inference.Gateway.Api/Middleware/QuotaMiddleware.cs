// <copyright file="QuotaMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.InferenceGateway.Application.Interfaces;

    /// <summary>
    /// Middleware that enforces quota limits for organizations and API keys.
    /// Handles rate limiting, budget enforcement, and quota exhaustion.
    /// </summary>
    public sealed class QuotaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<QuotaMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuotaMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate.</param>
        /// <param name="logger">The logger instance.</param>
        public QuotaMiddleware(
            RequestDelegate next,
            ILogger<QuotaMiddleware> logger)
        {
            this._next = next!;
            this._logger = logger!;
        }

        /// <summary>
        /// Invokes the middleware to enforce quota limits.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="tenantContext">The tenant context.</param>
        /// <param name="quotaService">The quota service.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IQuotaService quotaService)
        {
            try
            {
                if (ShouldSkipQuota(context, tenantContext))
                {
                    await this._next(context).ConfigureAwait(false);
                    return;
                }

                var quotaCheckRequest = BuildQuotaCheckRequest();
                var quotaResult = await CheckQuotaAsync(tenantContext, quotaService, quotaCheckRequest).ConfigureAwait(false);

                if (await this.HandleQuotaActionAsync(context, tenantContext, quotaResult).ConfigureAwait(false))
                {
                    return;
                }

                context.Items["QuotaResult"] = quotaResult;

                await this._next(context).ConfigureAwait(false);

                if (context.Response.StatusCode < 400)
                {
                    await IncrementUsageAsync(context, tenantContext, quotaService).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error occurred during quota enforcement");
                await this._next(context).ConfigureAwait(false);
            }
        }

        private static bool ShouldSkipQuota(HttpContext context, ITenantContext tenantContext)
        {
            if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/identity", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return tenantContext.OrganizationId == null;
        }

        private static QuotaCheckRequest BuildQuotaCheckRequest()
        {
            return new QuotaCheckRequest
            {
                MetricType = "requests",
                IncrementBy = 1,
                TimeGranularity = "minute",
                WindowType = WindowType.Sliding,
            };
        }

        private static Task<QuotaResult> CheckQuotaAsync(
            ITenantContext tenantContext,
            IQuotaService quotaService,
            QuotaCheckRequest quotaCheckRequest)
        {
            if (tenantContext.OrganizationId == null)
            {
                throw new InvalidOperationException("Organization context is required for quota enforcement.");
            }

            return tenantContext.UserId.HasValue
                ? quotaService.CheckUserQuotaAsync(
                    tenantContext.OrganizationId.Value,
                    tenantContext.UserId.Value,
                    quotaCheckRequest)
                : quotaService.CheckQuotaAsync(
                    tenantContext.OrganizationId.Value,
                    quotaCheckRequest);
        }

        private async Task<bool> HandleQuotaActionAsync(
            HttpContext context,
            ITenantContext tenantContext,
            QuotaResult quotaResult)
        {
            switch (quotaResult.Action)
            {
                case QuotaAction.Block:
                    this._logger.LogWarning(
                        "Request blocked due to quota exhaustion. OrgId: {OrgId}, Reason: {Reason}",
                        tenantContext.OrganizationId,
                        quotaResult.Reason);

                    context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            message = quotaResult.Reason ?? "Quota exceeded",
                            type = "quota_exceeded",
                            code = "QUOTA_EXHAUSTED",
                        },
                    }).ConfigureAwait(false);
                    return true;

                case QuotaAction.Throttle:
                    await this.WriteThrottleResponseAsync(context, tenantContext, quotaResult).ConfigureAwait(false);
                    return true;

                case QuotaAction.CreditCharge:
                    this._logger.LogInformation(
                        "Request will incur credit charge. OrgId: {OrgId}, Amount: {Amount}",
                        tenantContext.OrganizationId,
                        quotaResult.CreditCharge);

                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers["X-Credit-Charge"] = quotaResult.CreditCharge?.ToString("F4", CultureInfo.InvariantCulture) ?? "0";
                        return Task.CompletedTask;
                    });
                    return false;

                case QuotaAction.Allow:
                    return false;

                default:
                    return false;
            }
        }

        private Task WriteThrottleResponseAsync(
            HttpContext context,
            ITenantContext tenantContext,
            QuotaResult quotaResult)
        {
            this._logger.LogWarning(
                "Request throttled due to rate limit. OrgId: {OrgId}, Limit: {Limit}, Current: {Current}",
                tenantContext.OrganizationId,
                quotaResult.Details?.Limit,
                quotaResult.Details?.CurrentUsage);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            ApplyRateLimitHeaders(context, quotaResult);

            return context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    message = "Rate limit exceeded",
                    type = "rate_limit_exceeded",
                    code = "RATE_LIMIT_EXCEEDED",
                    retry_after = quotaResult.Details?.RetryAfter?.TotalSeconds,
                },
            });
        }

        private static void ApplyRateLimitHeaders(HttpContext context, QuotaResult quotaResult)
        {
            if (quotaResult.Details == null)
            {
                return;
            }

            context.Response.Headers["X-RateLimit-Limit"] = quotaResult.Details.Limit.ToString(CultureInfo.InvariantCulture);
            context.Response.Headers["X-RateLimit-Remaining"] = quotaResult.Details.Remaining.ToString(CultureInfo.InvariantCulture);
            context.Response.Headers["X-RateLimit-Reset"] = quotaResult.Details.WindowEnd.ToString("R", CultureInfo.InvariantCulture);

            if (quotaResult.Details.RetryAfter.HasValue)
            {
                context.Response.Headers["Retry-After"] = ((int)quotaResult.Details.RetryAfter.Value.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }
        }

        private static Task IncrementUsageAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IQuotaService quotaService)
        {
            if (tenantContext.OrganizationId == null)
            {
                throw new InvalidOperationException("Organization context is required for usage tracking.");
            }

            return quotaService.IncrementUsageAsync(
                tenantContext.OrganizationId.Value,
                new UsageMetrics
                {
                    UserId = tenantContext.UserId,
                    VirtualKeyId = tenantContext.ApiKeyId,
                    MetricType = "requests",
                    Value = 1,
                    Model = (context.Items["ModelName"] as string) ?? string.Empty,
                });
        }
    }
}
