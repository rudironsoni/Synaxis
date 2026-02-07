// <copyright file="AuditMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.Interfaces;

    /// <summary>
    /// Middleware that logs all API requests for audit and compliance purposes.
    /// Captures request/response details without logging sensitive data.
    /// </summary>
    public sealed class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate.</param>
        /// <param name="logger">The logger instance.</param>
        public AuditMiddleware(
            RequestDelegate next,
            ILogger<AuditMiddleware> logger)
        {
            this._next = next ?? throw new ArgumentNullException(nameof(next));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware to log request details.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="tenantContext">The tenant context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
#pragma warning disable MA0051 // Method is too long
        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext)
#pragma warning restore MA0051
        {
            // Skip audit logging for health checks and static resources
            if (context.Request.Path.StartsWithSegments("/health", StringComparison.Ordinal) ||
                context.Request.Path.StartsWithSegments("/openapi", StringComparison.Ordinal) ||
                context.Request.Path.Value?.Contains("swagger", StringComparison.OrdinalIgnoreCase) == true)
            {
                await this._next(context).ConfigureAwait(false);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();

            // Capture request details
            var requestMethod = context.Request.Method;
            var requestPath = context.Request.Path;
            var requestQuery = context.Request.QueryString.ToString();
            var clientIp = GetClientIpAddress(context);

            // Log request start
            this._logger.LogInformation(
                "API Request Started: {RequestId} {Method} {Path}{Query} from {ClientIp}",
                requestId,
                requestMethod,
                requestPath,
                requestQuery,
                clientIp);

            // Capture original response body stream
            var originalBodyStream = context.Response.Body;

            try
            {
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                // Process the request
                await this._next(context).ConfigureAwait(false);

                stopwatch.Stop();

                // Get response details
                var statusCode = context.Response.StatusCode;
                var responseSize = responseBodyStream.Length;

                // Log request completion
                LogLevel logLevel;
                if (statusCode >= 500)
                {
                    logLevel = LogLevel.Error;
                }
                else if (statusCode >= 400)
                {
                    logLevel = LogLevel.Warning;
                }
                else
                {
                    logLevel = LogLevel.Information;
                }

                this._logger.Log(
                    logLevel,
                    "API Request Completed: {RequestId} {Method} {Path} -> {StatusCode} in {Duration}ms | OrgId: {OrgId} | UserId: {UserId} | ApiKeyId: {ApiKeyId} | Region: {UserRegion} | CrossBorder: {CrossBorder} | Size: {ResponseSize} bytes",
                    requestId,
                    requestMethod,
                    requestPath,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    tenantContext.OrganizationId,
                    tenantContext.UserId,
                    tenantContext.ApiKeyId,
                    context.Items["UserRegion"],
                    context.Items["IsCrossBorder"],
                    responseSize);

                // Copy response body back to original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                this._logger.LogError(
                    ex,
                    "API Request Failed: {RequestId} {Method} {Path} after {Duration}ms | OrgId: {OrgId}",
                    requestId,
                    requestMethod,
                    requestPath,
                    stopwatch.ElapsedMilliseconds,
                    tenantContext.OrganizationId);

                // Re-throw with contextual information
                throw new InvalidOperationException($"API request {requestId} failed after {stopwatch.ElapsedMilliseconds}ms", ex);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
